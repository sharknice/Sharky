﻿namespace Sharky.MicroTasks
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
        EnemyCleanupService EnemyCleanupService;
        SharkyUnitData SharkyUnitData;

        ArmySplitter ArmySplitter;

        float lastFrameTime;

        public AttackTask(IMicroController microController, TargetingData targetingData, ActiveUnitData activeUnitData, DefenseService defenseService, MacroData macroData, AttackData attackData, TargetingService targetingService, MicroTaskData microTaskData, SharkyUnitData sharkyUnitData, ArmySplitter armySplitter, EnemyCleanupService enemyCleanupService, float priority)
        {
            MicroController = microController;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            DefenseService = defenseService;
            MacroData = macroData;
            AttackData = attackData;
            TargetingService = targetingService;
            MicroTaskData = microTaskData;
            SharkyUnitData = sharkyUnitData;
            ArmySplitter = armySplitter;
            EnemyCleanupService = enemyCleanupService;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();

            lastFrameTime = 0;
            Enabled = true;
        }

        private void RemoveTemporaryUnits()
        {
            UnitCommanders.RemoveAll(u => SharkyUnitData.UndeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.ArmyUnit))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

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

            var attackingEnemies = ActiveUnitData.SelfUnits.Where(u => u.Value.UnitClassifications.HasFlag(UnitClassification.ResourceCenter) || u.Value.UnitClassifications.HasFlag(UnitClassification.ProductionStructure)).SelectMany(u => u.Value.NearbyEnemies).Distinct();
            if (attackingEnemies.Any())
            {
                var armyPoint = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                var distanceToAttackPoint = Vector2.DistanceSquared(armyPoint, new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y));
                var closerEnemies = attackingEnemies.Where(e => Vector2.DistanceSquared(e.Position, armyPoint) < distanceToAttackPoint);
                if (!AttackData.Attacking)
                {
                    closerEnemies = attackingEnemies;
                }
                if (closerEnemies.Any())
                {
                    actions = ArmySplitter.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, UnitCommanders, false);
                    stopwatch.Stop();
                    lastFrameTime = stopwatch.ElapsedMilliseconds;
                    RemoveTemporaryUnits();
                    return actions;
                }
            }

            if (!hiddenBase && TargetingData.HiddenEnemyBase)
            {
                ResetClaimedUnits();
                if (MicroTaskData.ContainsKey(typeof(FindHiddenBaseTask).Name))
                {
                    MicroTaskData[typeof(FindHiddenBaseTask).Name].Enable();
                }
            }
            else if (!TargetingData.HiddenEnemyBase)
            {
                if (MicroTaskData.ContainsKey(typeof(FindHiddenBaseTask).Name) && MicroTaskData[typeof(FindHiddenBaseTask).Name].Enabled)
                {
                    MicroTaskData[typeof(FindHiddenBaseTask).Name].Disable();
                }
            }

            if (AttackData.Attacking)
            {
                actions = MicroController.Attack(UnitCommanders, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                RemoveTemporaryUnits();
                return actions;
            }
            else
            {
                var cleanupActions = EnemyCleanupService.CleanupEnemies(UnitCommanders, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                if (cleanupActions != null)
                {
                    actions = cleanupActions;
                }
                else
                {
                    actions = MicroController.Retreat(UnitCommanders, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                }

                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                RemoveTemporaryUnits();
                return actions;
            }
        }
    }
}
