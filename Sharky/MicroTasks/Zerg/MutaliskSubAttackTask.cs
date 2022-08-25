using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using Sharky.MicroTasks.Attack;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Zerg
{
    public class MutaliskSubAttackTask : AttackSubTask
    {
        TargetingService TargetingService;
        BaseData BaseData;
        MapDataService MapDataService;

        List<List<UnitCommander>> Groups;

        IMicroController MutaliskMicroController;

        private int groupSize;

        public bool AlwaysAttack { get; set; }
        public int GroupSize
        {
            get { return groupSize; }
            set
            {
                groupSize = value;
                ReformGroups();
            }
        }

        public int MinimumAttackSize { get; set; }

        public MutaliskSubAttackTask(DefaultSharkyBot defaultSharkyBot, IAttackTask parentTask, IMicroController mutaliskMicroController, int minimumAttackSize,
            float priority, bool enabled = false, bool alwaysAttack = true)
        {
            ParentTask = parentTask;

            MicroTaskData = defaultSharkyBot.MicroTaskData;
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            MapDataService = defaultSharkyBot.MapDataService;

            MutaliskMicroController = mutaliskMicroController;

            TargetingService = defaultSharkyBot.TargetingService;

            Priority = priority;
            Enabled = enabled;

            UnitCommanders = new List<UnitCommander>();
            Groups = new List<List<UnitCommander>>();
            GroupSize = 20;
            MinimumAttackSize = minimumAttackSize;

            ArmySplitter = new ArmySplitter(defaultSharkyBot);
            AlwaysAttack = alwaysAttack;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            ClaimCommanders(commanders.Values);
        }

        public override void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders)
        {
            ClaimCommanders(commanders);
        }

        void ClaimCommanders(IEnumerable<UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Claimed)
                {
                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_MUTALISK)
                    {
                        commander.Claimed = true;
                        commander.UnitRole = UnitRole.Attack;
                        commander.AlwaysSpam = true;

                        UnitCommanders.Add(commander);

                        var group = GetGroupLookingForMore();
                        group.Add(commander);
                    }
                }
            }
        }

        List<UnitCommander> GetGroupLookingForMore()
        {
            var group = Groups.FirstOrDefault(g => g.Count() < GroupSize);
            if (group == null)
            {
                group = new List<UnitCommander>();
                Groups.Add(group);
            }
            return group;
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            var count = 0;
            foreach (var tag in deadUnits)
            {
                count = UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                foreach (var group in Groups)
                {
                    if (group.Any(c => c.UnitCalculation.Unit.Tag == tag && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM))
                    {
                        Disable();
                        return;
                    }
                    group.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                }
            }

            if (count > 0)
            {
                Groups.RemoveAll(g => g.Count() == 0);
                ReformGroups();
            }
        }

        public override void ResetClaimedUnits()
        {
            Groups.Clear();
            base.ResetClaimedUnits();
        }

        private void ReformGroups()
        {
            // TODO: combine or divide groups if needed
        }

        void UpdateStates()
        {
            foreach (var group in Groups)
            {
                if (group.Count() < MinimumAttackSize)
                {
                    foreach (var commander in group)
                    {
                        commander.UnitRole = UnitRole.Hide;
                    }
                }
                else if (group.Sum(c => c.UnitCalculation.Unit.Health) < group.Sum(c => c.UnitCalculation.Unit.HealthMax) * .75f)
                {
                    foreach (var commander in group)
                    {
                        commander.UnitRole = UnitRole.Regenerate;
                    }
                }
                else
                {
                    var groupCenter = TargetingService.GetArmyPoint(group);
                    var groupVector = new Vector2(groupCenter.X, groupCenter.Y);
                    foreach (var commander in group)
                    {
                        if (commander.UnitCalculation.Unit.Health <= commander.UnitCalculation.Unit.HealthMax * .75f)
                        {
                            commander.UnitRole = UnitRole.Regenerate;
                            commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.FullRetreat;
                        }
                        else if ((commander.UnitRole == UnitRole.Regenerate || commander.UnitRole == UnitRole.Hide) && commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax * .9f)
                        {
                            commander.UnitRole = UnitRole.Regroup;
                        }
                        else if (commander.UnitRole == UnitRole.Regroup && Vector2.DistanceSquared(commander.UnitCalculation.Position, groupVector) < 100)
                        {
                            commander.UnitRole = UnitRole.Attack;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> Attack(Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {   
                if (group?.FirstOrDefault() == null)
                {
                    continue;
                }

                var groupCenter = TargetingService.GetArmyPoint(group);

                var retreatSpot = GetRegenerationSpot(group, defensePoint, armyPoint, frame);

                if (group.FirstOrDefault().UnitCalculation.TargetPriorityCalculation.AirWinnability > 1)
                {
                    if (group.Count(c => c.UnitRole == UnitRole.Attack) >= MinimumAttackSize)
                    {
                        actions.AddRange(MutaliskMicroController.Attack(group, attackPoint, retreatSpot, groupCenter, frame));
                    }
                    else
                    {
                        actions.AddRange(MutaliskMicroController.Retreat(group, retreatSpot, groupCenter, frame));
                    }
                }
                else
                {
                    actions.AddRange(MutaliskMicroController.Retreat(group, retreatSpot, groupCenter, frame));
                }
            }

            return actions;
        }

        Point2D GetRegenerationSpot(List<UnitCommander> group, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            var retreatSpot = defensePoint;
            var firstUnit = group.FirstOrDefault();
            if (group.FirstOrDefault().UnitRole == UnitRole.Hide)
            {
                // TODO: find best hiding spot near own base
                return BaseData.MainBase.BehindMineralLineLocation;
            } 
            else
            {
                var closestSafeBase = BaseData?.EnemyBaseLocations?.FirstOrDefault(b => b.ResourceCenter == null && !MapDataService.InEnemyVision(b.Location));
                if (closestSafeBase != null)
                {
                    return closestSafeBase.Location;
                }
            }

            return retreatSpot;
        }

        public override IEnumerable<SC2APIProtocol.Action> Retreat(Point2D defensePoint, Point2D armyPoint, int frame)
        {
            if (AlwaysAttack) { return Attack(TargetingData.AttackPoint, defensePoint, armyPoint, frame); }

            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var retreatPoint = defensePoint;
                if (group.FirstOrDefault().UnitRole == UnitRole.Hide)
                {
                    // TODO: find best hiding spot near own base
                    retreatPoint = BaseData.MainBase.BehindMineralLineLocation;
                }

                var groupCenter = TargetingService.GetArmyPoint(group);
                actions.AddRange(MutaliskMicroController.Retreat(group, retreatPoint, groupCenter, frame));
            }

            return actions;
        }

        public override IEnumerable<SC2APIProtocol.Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var groupCenter = TargetingService.GetArmyPoint(group);
                actions.AddRange(MutaliskMicroController.Support(group, mainUnits, attackPoint, defensivePoint, groupCenter, frame));
            }

            return actions;
        }

        public override IEnumerable<SC2APIProtocol.Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            if (AlwaysAttack) { return Support(mainUnits, attackPoint, defensivePoint, armyPoint, frame); }

            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var groupCenter = TargetingService.GetArmyPoint(group);
                actions.AddRange(MutaliskMicroController.Support(group, mainUnits, defensivePoint, defensivePoint, groupCenter, frame));
            }

            return actions;
        }
    }
}
