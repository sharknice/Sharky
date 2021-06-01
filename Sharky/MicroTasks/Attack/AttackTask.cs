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

        ArmySplitter ArmySplitter;

        float lastFrameTime;

        public AttackTask(IMicroController microController, TargetingData targetingData, ActiveUnitData activeUnitData, DefenseService defenseService, MacroData macroData, AttackData attackData, TargetingService targetingService, MicroTaskData microTaskData, ArmySplitter armySplitter, float priority)
        {
            MicroController = microController;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            DefenseService = defenseService;
            MacroData = macroData;
            AttackData = attackData;
            TargetingService = targetingService;
            MicroTaskData = microTaskData;
            ArmySplitter = armySplitter;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();

            lastFrameTime = 0;
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
                    actions = ArmySplitter.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, UnitCommanders, false);
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
    }
}
