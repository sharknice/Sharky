namespace Sharky.MicroTasks.Attack
{
    public class AdvancedAttackService : IAttackService
    {
        public IMicroTask AttackTask { get; set; }

        protected AttackData AttackData;
        protected ActiveUnitData ActiveUnitData;
        protected TargetPriorityService TargetPriorityService;
        protected TargetingData TargetingData;
        protected MacroData MacroData;
        protected BaseData BaseData;
        protected TargetingService TargetingService;
        protected DebugService DebugService;
        protected MapDataService MapDataService;
        protected SharkyOptions SharkyOptions;

        IEnumerable<UnitCalculation>? EnemiesNearArmy;

        int TemporarilyRetreatStartFrame { get; set; }
        float TemporarilyRetreatDuration { get; set; }


        public AdvancedAttackService(DefaultSharkyBot defaultSharkyBot, IMicroTask attackTask)
        {
            AttackData = defaultSharkyBot.AttackData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            TargetPriorityService = defaultSharkyBot.TargetPriorityService;
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
            BaseData = defaultSharkyBot.BaseData;
            TargetingService = defaultSharkyBot.TargetingService;
            DebugService = defaultSharkyBot.DebugService;
            MapDataService = defaultSharkyBot.MapDataService;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            AttackTask = attackTask;
        }

        public void TemporarilyRetreat(int frame, float seconds)
        {
            TemporarilyRetreatStartFrame = frame;
            TemporarilyRetreatDuration = seconds * SharkyOptions.FramesPerSecond;
        }

        public bool Attack()
        {
            UpdateState();
            UpdateAttackPoint();

            if (TargetingData.AttackState == AttackState.Kill || TargetingData.AttackState == AttackState.Contain)
            {
                return true;
            }
            return false;
        }

        void UpdateState()
        {
            var targetPriority = CalculateTargetPriority();
            AttackData.TargetPriorityCalculation = targetPriority;

            if (AttackTask.UnitCommanders.Count() + ((AdvancedAttackTask)AttackTask).SubTasks.Values.Sum(t => t.UnitCommanders.Count()) < 1)
            {
                TargetingData.AttackState = AttackState.Retreat;
                DebugService.DrawText("Retreating: no army");
                return;
            }

            if (TemporarilyRetreatStartFrame + TemporarilyRetreatDuration > MacroData.Frame)
            {
                TargetingData.AttackState = AttackState.Retreat;
            }

            if (AttackEdgeCase())
            {
                TargetingData.AttackState = AttackState.Kill;
                return;
            }

            if (AttackData.RequireBank)
            {
                if (MacroData.Minerals < AttackData.RequiredMineralBank || MacroData.VespeneGas < AttackData.RequiredVespeneBank)
                {
                    TargetingData.AttackState = AttackState.Retreat;
                    DebugService.DrawText("Retreating: require 2000 mineral and 1000 vespene gas bank");
                    return;
                }
            }

            if (AttackData.RequireDetection && !AttackTask.UnitCommanders.Any(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) || c.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster)))
            {
                TargetingData.AttackState = AttackState.Retreat;
                DebugService.DrawText("Retreating: need detection");
                return;
            }

            if (AttackData.RequireMaxOut)
            {
                if (TargetingData.AttackState == AttackState.Kill && (MacroData.FoodUsed >= AttackData.ArmyFoodRetreat || targetPriority.Overwhelm || targetPriority.OverallWinnability > AttackData.KillTrigger))
                {
                    DebugService.DrawText("Attacking: continuing maxout attack");
                    return;
                }

                if (MacroData.FoodUsed < 190)
                {
                    TargetingData.AttackState = AttackState.Retreat;
                    DebugService.DrawText("Retreating: require maxed out supply");
                    return;
                }
            }

            if (TargetingData.AttackState == AttackState.Contain)
            {
                var containTargetPriority = CalculateContainTargetPriority();
                if (targetPriority.Overwhelm || targetPriority.OverallWinnability > AttackData.KillTrigger)
                {
                    TargetingData.AttackState = AttackState.Kill;
                }
                else if (containTargetPriority.OverallWinnability <= AttackData.RetreatTrigger)
                {
                    TargetingData.AttackState = AttackState.Retreat;
                }
                else if (AttackData.ContainBelowKill)
                {
                    var targetVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
                    var armyVector = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                    var armyDistance = Vector2.DistanceSquared(armyVector, targetVector);
                    if (EnemiesNearArmy != null && EnemiesNearArmy.Any(e => MapDataService.SelfVisible(e.Unit.Pos) && Vector2.DistanceSquared(e.Position, targetVector) < armyDistance && Vector2.DistanceSquared(e.Position, armyVector) < Vector2.DistanceSquared(targetVector, armyVector)))
                    {
                        TargetingData.AttackState = AttackState.Regroup;
                    }
                }
                else if (targetPriority.OverallWinnability <= AttackData.RetreatTrigger)
                {
                    TargetingData.AttackState = AttackState.Retreat;
                }
            }
            else if (TargetingData.AttackState == AttackState.Regroup)
            {
                var armyDistance = Vector2.DistanceSquared(new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y), new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y));
                if (armyDistance < 100)
                {
                    TargetingData.AttackState = AttackState.Contain;
                }
            }
            else if (TargetingData.AttackState == AttackState.Kill)
            {
                if (targetPriority.OverallWinnability <= AttackData.KillTrigger)
                {
                    if (targetPriority.OverallWinnability <= AttackData.RetreatTrigger)
                    {
                        TargetingData.AttackState = AttackState.Retreat;
                    }
                    else if (AttackData.ContainBelowKill)
                    {
                        TargetingData.AttackState = AttackState.Contain;
                    }
                }
            }
            else
            {
                if (targetPriority.Overwhelm && AttackData.AttackWhenOverwhelm || targetPriority.OverallWinnability > AttackData.KillTrigger)
                {
                    TargetingData.AttackState = AttackState.Kill;
                }
                else if (targetPriority.OverallWinnability >= AttackData.ContainTrigger)
                {
                    TargetingData.AttackState = AttackState.Contain;
                }
            }

            DebugService.DrawText($"{TargetingData.AttackState}: O:{targetPriority.OverallWinnability:0.00}, G:{targetPriority.GroundWinnability:0.00}, A:{targetPriority.AirWinnability:0.00}");
        }

        protected virtual bool AttackEdgeCase()
        {
            if (MacroData.Minerals > AttackData.MaxMineralBankEdgeCase)
            {
                TargetingData.AttackState = AttackState.Kill;
                DebugService.DrawText($"Attacking: > {AttackData.MaxMineralBankEdgeCase} max mineral bank");
                return true;
            }

            if (AttackData.AttackWhenMaxedOut && MacroData.FoodUsed > 185)
            {
                TargetingData.AttackState = AttackState.Kill;
                DebugService.DrawText("Attacking: > 185 supply");
                return true;
            }

            if (ActiveUnitData.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.Worker)) == 0)
            {
                TargetingData.AttackState = AttackState.Kill;
                DebugService.DrawText("Attacking: no workers");
                return true;
            }

            if (ActiveUnitData.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter)) == 0)
            {
                TargetingData.AttackState = AttackState.Kill;
                DebugService.DrawText("Attacking: no base");
                return true;
            }

            if (BaseData.SelfBases.All(b => !b.MineralMiningInfo.Any(m => m.Workers.Any())))
            {
                TargetingData.AttackState = AttackState.Kill;
                DebugService.DrawText("Attacking: not mining");
                return true;
            }

            return false;
        }

        void UpdateAttackPoint()
        {
            if (TargetingData.AttackState == AttackState.Contain)
            {
                var nextEnemyExpansion = BaseData.EnemyBaseLocations.FirstOrDefault(b => !BaseData.EnemyBases.Any(e => b.Location == e.Location));
                if (nextEnemyExpansion != null)
                {
                    TargetingData.AttackPoint = nextEnemyExpansion.Location;
                }
                else
                {
                    TargetingData.AttackPoint = TargetingData.ForwardDefensePoint;
                }
            }
            else
            {
                TargetingData.AttackPoint = TargetingService.UpdateAttackPoint(AttackData.ArmyPoint, TargetingData.AttackPoint);
            }
        }

        TargetPriorityCalculation CalculateTargetPriority()
        {
            var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && Vector2.DistanceSquared(attackVector, e.Position) < 625)); // every enemy unit no matter where it is, defensive structures within 25 range of attack point

            return TargetPriorityService.CalculateTargetPriority(AttackTask.UnitCommanders.Select(c => c.UnitCalculation), enemyUnits);
        }

        TargetPriorityCalculation CalculateContainTargetPriority()
        {
            var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(e => (e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN) && Vector2.DistanceSquared(attackVector, e.Position) < 625); // every enemy unit and defensive structure within 25 range of attack point
            EnemiesNearArmy = AttackTask.UnitCommanders.SelectMany(c => c.UnitCalculation.NearbyEnemies); // and enemies nearby because we need to fight through them to get there

            return TargetPriorityService.CalculateTargetPriority(AttackTask.UnitCommanders.Select(c => c.UnitCalculation), enemyUnits.Concat(EnemiesNearArmy).Distinct());
        }
    }
}
