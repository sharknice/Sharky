using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Attack
{
    public class ArmySplitter
    {
        AttackData AttackData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;

        DefenseService DefenseService;
        TargetingService TargetingService;
        TerranWallService TerranWallService;
        TargetPriorityService TargetPriorityService;

        IMicroController MicroController;

        float LastSplitFrame;

        public List<ArmySplits> ArmySplits { get; private set; }
        List<UnitCommander> AvailableCommanders;

        public ArmySplitter(DefaultSharkyBot defaultSharkyBot)
        {
            AttackData = defaultSharkyBot.AttackData;
            TargetingData = defaultSharkyBot.TargetingData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;

            DefenseService = defaultSharkyBot.DefenseService;
            TargetingService = defaultSharkyBot.TargetingService;
            TerranWallService = defaultSharkyBot.TerranWallService;
            TargetPriorityService = defaultSharkyBot.TargetPriorityService;

            MicroController = defaultSharkyBot.MicroController;

            LastSplitFrame = -1000;
        }

        public List<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders, bool defendToDeath)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var winnableDefense = false;

            if (LastSplitFrame + 25 < frame)
            {
                ReSplitArmy(frame, closerEnemies, attackPoint, unitCommanders, defendToDeath);
                LastSplitFrame = frame;
            }

            foreach (var split in ArmySplits)
            {
                if (split.SelfGroup.Count() > 0)
                {
                    var groupPoint = TargetingService.GetArmyPoint(split.SelfGroup);
                    if (split.SelfGroup.Count() == 0)
                    {
                        groupPoint = null;
                    }
                    foreach (var commander in split.SelfGroup)
                    {
                        commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                    }
                    var leaders = split.SelfGroup.Where(c => c.UnitRole == UnitRole.Leader);
                    if (leaders.Any())
                    {
                        actions.AddRange(MicroController.Attack(leaders, split.EnemyGroup.FirstOrDefault().Position.ToPoint2D(), TargetingData.ForwardDefensePoint, groupPoint, frame));
                        var others = split.SelfGroup.Where(c => c.UnitRole != UnitRole.Leader);
                        if (others.Any())
                        {
                            var mainUnit = leaders.FirstOrDefault();
                            var supportAttackPoint = new Point2D { X = mainUnit.UnitCalculation.Position.X, Y = mainUnit.UnitCalculation.Position.Y };
                            actions.AddRange(MicroController.Support(others, leaders, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                        }
                    }
                    else
                    {
                        actions.AddRange(MicroController.Attack(split.SelfGroup, split.EnemyGroup.FirstOrDefault().Position.ToPoint2D(), TargetingData.ForwardDefensePoint, groupPoint, frame));
                    }

                    winnableDefense = true;
                }
            }

            if (AvailableCommanders.Count() > 0)
            {
                var groupPoint = TargetingService.GetArmyPoint(AvailableCommanders);
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(AvailableCommanders, attackPoint, TargetingData.ForwardDefensePoint, groupPoint, frame));
                }
                else
                {
                    var defensiveVector = new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y);
                    var shieldBattery = ActiveUnitData.SelfUnits.Values.Where(u => ((u.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && u.Unit.Energy > 5) || (u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && u.Unit.Shield > 5)) && u.Unit.IsPowered && u.Unit.BuildProgress == 1).OrderBy(u => Vector2.DistanceSquared(u.Position, defensiveVector)).FirstOrDefault();
                    if (shieldBattery != null)
                    {
                        actions.AddRange(MicroController.Retreat(AvailableCommanders, shieldBattery.Position.ToPoint2D(), groupPoint, frame));
                    }
                    else
                    {
                        if (EnemyData.SelfRace == Race.Terran && TerranWallService != null && TerranWallService.MainWallComplete())
                        {
                            actions.AddRange(MicroController.Retreat(AvailableCommanders, TargetingData.ForwardDefensePoint, groupPoint, frame));
                        }
                        else
                        {
                            actions.AddRange(MicroController.Retreat(AvailableCommanders, TargetingData.MainDefensePoint, groupPoint, frame));
                        }
                    }                 
                }
            }

            return actions;
        }

        void ReSplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders, bool defendToDeath)
        {
            ArmySplits = new List<ArmySplits>();
            var enemyGroups = DefenseService.GetEnemyGroups(closerEnemies);
            AvailableCommanders = unitCommanders.ToList();
            foreach (var enemyGroup in enemyGroups)
            {
                var selfGroup = DefenseService.GetDefenseGroup(enemyGroup, AvailableCommanders, defendToDeath);
                if (selfGroup.Count() > 0)
                {
                    AvailableCommanders.RemoveAll(a => selfGroup.Any(s => a.UnitCalculation.Unit.Tag == s.UnitCalculation.Unit.Tag));
                }
                ArmySplits.Add(new ArmySplits { EnemyGroup = enemyGroup, SelfGroup = selfGroup });
            }


            if (!AttackData.Attacking && AvailableCommanders.Any())
            {
                foreach (var split in ArmySplits)
                {
                    var additions = DefenseService.OverwhelmSplit(split, AvailableCommanders);
                    split.SelfGroup.AddRange(additions);
                    AvailableCommanders.RemoveAll(a => additions.Any(s => s.UnitCalculation.Unit.Tag == a.UnitCalculation.Unit.Tag));
                }
            }
        }
    }
}
