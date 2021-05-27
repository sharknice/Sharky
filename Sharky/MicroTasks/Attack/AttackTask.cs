using SC2APIProtocol;
using Sharky.MicroControllers;
using Sharky.MicroTasks.Attack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class AttackTask : MicroTask
    {
        IMicroController MicroController;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        DefenseService DefenseService;
        MacroData MacroData;
        AttackData AttackData;
        TargetingService TargetingService;
        MicroTaskData MicroTaskData;

        float lastFrameTime;
        float LastSplitFrame;

        List<ArmySplits> ArmySplits;
        List<UnitCommander> AvailableCommanders;

        public AttackTask(IMicroController microController, TargetingData targetingData, ActiveUnitData activeUnitData, DefenseService defenseService, MacroData macroData, AttackData attackData, TargetingService targetingService, MicroTaskData microTaskData, float priority)
        {
            MicroController = microController;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            DefenseService = defenseService;
            MacroData = macroData;
            AttackData = attackData;
            TargetingService = targetingService;
            MicroTaskData = microTaskData;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();

            lastFrameTime = 0;
            LastSplitFrame = -1000;
            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (lastFrameTime > 5)
            {
                lastFrameTime = 0;
                return actions;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var hiddenBase = TargetingData.HiddenEnemyBase;
            AttackData.ArmyPoint = TargetingService.GetArmyPoint(UnitCommanders);
            TargetingData.AttackPoint = TargetingService.UpdateAttackPoint(AttackData.ArmyPoint, TargetingData.AttackPoint);

            if (!AttackData.CustomAttackFunction)
            {
                if (AttackData.Attacking)
                {
                    if (MacroData.FoodUsed < AttackData.ArmyFoodRetreat)
                    {
                        AttackData.Attacking = false;
                    }
                }
                else
                {
                    AttackData.Attacking = MacroData.FoodArmy >= AttackData.ArmyFoodAttack || MacroData.FoodUsed > 190;
                }
            }

            var attackingEnemies = ActiveUnitData.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.Value.UnitClassifications.Contains(UnitClassification.ProductionStructure)).SelectMany(u => u.Value.NearbyEnemies).Distinct();
            if (attackingEnemies.Count() > 0)
            {
                var armyPoint = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                var distanceToAttackPoint = Vector2.DistanceSquared(armyPoint, new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y));
                var closerEnemies = attackingEnemies.Where(e => Vector2.DistanceSquared(e.Position, armyPoint) < distanceToAttackPoint);
                if (!AttackData.Attacking)
                {
                    closerEnemies = attackingEnemies;
                }
                if (closerEnemies.Count() > 0)
                {
                    actions = SplitArmy(frame, closerEnemies, TargetingData.AttackPoint);
                    stopwatch.Stop();
                    lastFrameTime = stopwatch.ElapsedMilliseconds;
                    return actions;
                }
            }

            if (!hiddenBase && TargetingData.HiddenEnemyBase)
            {
                ResetClaimedUnits();
                if (MicroTaskData.MicroTasks.ContainsKey("FindHiddenBaseTask"))
                {
                    MicroTaskData.MicroTasks["FindHiddenBaseTask"].Enable();
                }
            }
            else if (!TargetingData.HiddenEnemyBase)
            {
                if (MicroTaskData.MicroTasks.ContainsKey("FindHiddenBaseTask") && MicroTaskData.MicroTasks["FindHiddenBaseTask"].Enabled)
                {
                    MicroTaskData.MicroTasks["FindHiddenBaseTask"].Disable();
                }
            }

            if (AttackData.Attacking)
            {
                actions = MicroController.Attack(UnitCommanders, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }
            else
            {
                actions = MicroController.Retreat(UnitCommanders, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }
        }

        List<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (LastSplitFrame + 100 < frame)
            {
                ReSplitArmy(frame, closerEnemies, attackPoint);
                LastSplitFrame = frame;
            }

            foreach (var split in ArmySplits)
            {
                if (split.SelfGroup.Count() > 0)
                {
                    var groupVectors = split.SelfGroup.Select(u => u.UnitCalculation.Position);
                    var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                    var defensePoint = new Point2D { X = split.EnemyGroup.FirstOrDefault().Unit.Pos.X, Y = split.EnemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(split.SelfGroup, defensePoint, TargetingData.ForwardDefensePoint, groupPoint, frame));
                }
            }

            if (AvailableCommanders.Count() > 0)
            {
                var groupVectors = AvailableCommanders.Select(u => u.UnitCalculation.Position);
                var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(AvailableCommanders, attackPoint, TargetingData.ForwardDefensePoint, groupPoint, frame));
                }
                else
                {
                    actions.AddRange(MicroController.Attack(AvailableCommanders, new Point2D { X = closerEnemies.FirstOrDefault().Unit.Pos.X, Y = closerEnemies.FirstOrDefault().Unit.Pos.Y }, TargetingData.ForwardDefensePoint, groupPoint, frame));
                }
            }

            return actions;
        }

        void ReSplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint)
        {
            ArmySplits = new List<ArmySplits>();
            var enemyGroups = DefenseService.GetEnemyGroups(closerEnemies);
            AvailableCommanders = UnitCommanders.ToList();
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
