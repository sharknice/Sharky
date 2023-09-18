namespace Sharky.Managers
{
    public class AttackDataManager : SharkyManager
    {
        AttackData AttackData;
        ActiveUnitData ActiveUnitData;
        public IMicroTask AttackTask { get; set; }
        TargetPriorityService TargetPriorityService;
        TargetingData TargetingData;
        MacroData MacroData;
        BaseData BaseData;

        DebugService DebugService;

        public AttackDataManager(AttackData attackData, ActiveUnitData activeUnitData, IMicroTask attackTask, TargetPriorityService targetPriorityService, TargetingData targetingData, MacroData macroData, BaseData baseData, DebugService debugService)
        {
            AttackData = attackData;
            ActiveUnitData = activeUnitData;
            AttackTask = attackTask;
            TargetPriorityService = targetPriorityService;
            TargetingData = targetingData;
            MacroData = macroData;
            BaseData = baseData;

            DebugService = debugService;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            AttackData.CustomAttackFunction = true;
            AttackData.Attacking = false;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            if (!AttackData.UseAttackDataManager)
            {
                return null;
            }

            if (MacroData.FoodUsed > 185)
            {
                AttackData.Attacking = true;
                DebugService.DrawText("Attacking: > 185 supply");
                return null;
            }

            if (ActiveUnitData.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.Worker)) == 0)
            {
                AttackData.Attacking = true;
                DebugService.DrawText("Attacking: no workers");
                return null;
            }

            if (ActiveUnitData.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter)) == 0)
            {
                AttackData.Attacking = true;
                DebugService.DrawText("Attacking: not mining minerals");
                return null;
            }

            // attack if no workers mining minerals or if no bases
            if (!BaseData.SelfBases.Any(b => b.MineralMiningInfo.Any(m => m.Workers.Any())))
            {
                AttackData.Attacking = true;
                DebugService.DrawText("Attacking: no base");
                return null;
            }

            if (!AttackTask.UnitCommanders.Any())
            {
                AttackData.Attacking = false;
                if (AttackData.TargetPriorityCalculation != null)
                {
                    AttackData.TargetPriorityCalculation.OverallWinnability = 0;
                }
                DebugService.DrawText("Not Attacking: no attacking army");
                return null;
            }

            var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(e => (e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && Vector2.DistanceSquared(new Vector2(TargetingData.MainDefensePoint.X, TargetingData.MainDefensePoint.Y), e.Position) > 400)
            || (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && Vector2.DistanceSquared(attackVector, e.Position) < 625));

            if (!enemyUnits.Any())
            {
                var priority = TargetPriorityService.CalculateTargetPriority(AttackTask.UnitCommanders.Select(c => c.UnitCalculation), ActiveUnitData.EnemyUnits.Values.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)));
                AttackData.TargetPriorityCalculation = priority;
                AttackData.Attacking = true;
                DebugService.DrawText("Attacking: no enemy army defending");
                return null;
            }

            var targetPriority = TargetPriorityService.CalculateTargetPriority(AttackTask.UnitCommanders.Select(c => c.UnitCalculation), enemyUnits);
            AttackData.TargetPriorityCalculation = targetPriority;

            var overallTrigger = AttackData.RetreatTrigger;
            if (!AttackData.Attacking)
            {
                overallTrigger = AttackData.AttackTrigger;
            }

            if (targetPriority.OverallWinnability >= overallTrigger || targetPriority.GroundWinnability > 2 || targetPriority.AirWinnability > 2)
            {
                AttackData.Attacking = true;
                DebugService.DrawText($"Attacking: O:{targetPriority.OverallWinnability:0.00}, G:{targetPriority.GroundWinnability:0.00}, A:{targetPriority.AirWinnability:0.00}");
            }
            else
            {
                AttackData.Attacking = false;
                DebugService.DrawText($"Not Attacking: O:{targetPriority.OverallWinnability:0.00}, G:{targetPriority.GroundWinnability:0.00}, A:{targetPriority.AirWinnability:0.00}");
            }

            return null;
        }
    }
}
