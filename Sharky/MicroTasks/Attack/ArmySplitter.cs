using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
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

        DefenseService DefenseService;
        TargetingService TargetingService;
        TerranWallService TerranWallService;

        IMicroController MicroController;

        float LastSplitFrame;

        List<ArmySplits> ArmySplits;
        List<UnitCommander> AvailableCommanders;

        public ArmySplitter(DefaultSharkyBot defaultSharkyBot)
        {
            AttackData = defaultSharkyBot.AttackData;
            TargetingData = defaultSharkyBot.TargetingData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;

            DefenseService = defaultSharkyBot.DefenseService;
            TargetingService = defaultSharkyBot.TargetingService;
            TerranWallService = defaultSharkyBot.TerranWallService;

            MicroController = defaultSharkyBot.MicroController;

            LastSplitFrame = -1000;
        }

        public ArmySplitter(AttackData attackData, TargetingData targetingData, ActiveUnitData activeUnitData,
            DefenseService defenseService, TargetingService targetingService, TerranWallService terranWallService, IMicroController microController)
        {
            AttackData = attackData;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;

            DefenseService = defenseService;
            TargetingService = targetingService;
            TerranWallService = terranWallService;

            MicroController = microController;

            LastSplitFrame = -1000;
        }

        public List<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders, bool defendToDeath)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var winnableDefense = false;

            if (LastSplitFrame + 25 < frame)
            {
                ReSplitArmy(frame, closerEnemies, attackPoint, unitCommanders);
                LastSplitFrame = frame;
            }

            foreach (var split in ArmySplits)
            {
                if (split.SelfGroup.Count() > 0)
                {
                    var groupPoint = TargetingService.GetArmyPoint(AvailableCommanders);
                    if (AvailableCommanders.Count() == 0)
                    {
                        groupPoint = null;
                    }
                    var defensePoint = new Point2D { X = split.EnemyGroup.FirstOrDefault().Unit.Pos.X, Y = split.EnemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(split.SelfGroup, defensePoint, TargetingData.ForwardDefensePoint, groupPoint, frame));

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
                    if (winnableDefense || defendToDeath)
                    {
                        actions.AddRange(MicroController.Attack(AvailableCommanders, new Point2D { X = closerEnemies.FirstOrDefault().Unit.Pos.X, Y = closerEnemies.FirstOrDefault().Unit.Pos.Y }, TargetingData.ForwardDefensePoint, groupPoint, frame));
                    }
                    else
                    {
                        var defensiveVector = new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y);
                        var shieldBattery = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && u.Unit.IsPowered && u.Unit.BuildProgress == 1 && u.Unit.Energy > 5).OrderBy(u => Vector2.DistanceSquared(u.Position, defensiveVector)).FirstOrDefault();
                        if (shieldBattery != null)
                        {
                            actions.AddRange(MicroController.Retreat(AvailableCommanders, new Point2D { X = shieldBattery.Position.X, Y = shieldBattery.Position.Y }, groupPoint, frame));
                        }
                        else
                        {
                            if (TerranWallService != null && TerranWallService.MainWallComplete())
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
            }

            return actions;
        }

        void ReSplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders)
        {
            ArmySplits = new List<ArmySplits>();
            var enemyGroups = DefenseService.GetEnemyGroups(closerEnemies);
            AvailableCommanders = unitCommanders.ToList();
            foreach (var enemyGroup in enemyGroups)
            {
                var selfGroup = DefenseService.GetDefenseGroup(enemyGroup, AvailableCommanders);
                if (selfGroup.Count() > 0)
                {
                    AvailableCommanders.RemoveAll(a => selfGroup.Any(s => a.UnitCalculation.Unit.Tag == s.UnitCalculation.Unit.Tag));
                }
                ArmySplits.Add(new ArmySplits { EnemyGroup = enemyGroup, SelfGroup = selfGroup });
            }
        }
    }
}
