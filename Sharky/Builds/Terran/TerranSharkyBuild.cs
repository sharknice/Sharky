namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        protected TargetingData TargetingData;
        protected MapDataService MapDataService;
        protected OrbitalManager OrbitalManager;
        protected SharkyUnitData SharkyUnitData;
        protected BaseData BaseData;
        protected UnitRequestCancellingService UnitRequestCancellingService;

        protected float ScanAttackPointTime { get; set; }
        protected float ScanNextEnemyBaseTime { get; set; }

        public TerranSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            OrbitalManager = defaultSharkyBot.OrbitalManager;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BaseData = defaultSharkyBot.BaseData;
            UnitRequestCancellingService = defaultSharkyBot.UnitRequestCancellingService;

            ScanAttackPointTime = 120f;
            ScanNextEnemyBaseTime = 120f;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.WallOffType = WallOffType.Terran;
            TargetingData.WallOffBasePosition = WallOffBasePosition.Main;
        }

        protected void SendScvForFirstDepot(int frame)
        {
            if (MacroData.FoodUsed == 13 && MacroData.Minerals > 80 && UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_SUPPLYDEPOT) == 0)
            {
                if (MapDataService != null && MapDataService.MapData.WallData != null)
                {
                    var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
                    if (wallData != null && wallData.Depots != null)
                    {
                        var point = wallData.Depots.FirstOrDefault();
                        if (point != null)
                        {
                            PrePositionBuilderTask.SendBuilder(point, frame);
                            return;
                        }
                    }
                }
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendScvForFirstBarracks(int frame)
        {
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) == 1 && UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_BARRACKS) == 0)
            {
                if (MapDataService != null && MapDataService.MapData.WallData != null)
                {
                    var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
                    if (wallData != null && wallData.Production != null)
                    {
                        var point = wallData.Production.FirstOrDefault();
                        if (point != null)
                        {
                            PrePositionBuilderTask.SendBuilder(point, frame);
                            return;
                        }
                    }
                }
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendScvForCommandCenter(int frame)
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) == 1 && MacroData.Minerals > 275)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }

        protected bool CommandCenterScvKilled()
        {
            var building = ActiveUnitData.Commanders.FirstOrDefault(c => c.Value.UnitCalculation.Unit.BuildProgress < 1 && c.Value.UnitCalculation.Unit.BuildProgress > 0 && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER && c.Value.UnitCalculation.Unit.BuildProgress == c.Value.UnitCalculation.PreviousUnit.BuildProgress);
            if (building.Value != null)
            {
                var scvs = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
                var buildingScv = scvs.FirstOrDefault(c => c.UnitCalculation.Unit.Orders.Any(o => o.TargetUnitTag == building.Key || (o.TargetWorldSpacePos != null && o.TargetWorldSpacePos.X == building.Value.UnitCalculation.Position.X && o.TargetWorldSpacePos.Y == building.Value.UnitCalculation.Position.Y)));
                if (buildingScv == null)
                {
                    return true;
                }
            }

            return false;
        }

        protected void StopScvProductionForOrbitals()
        {
            if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] > UnitCountService.BuildingsDoneAndInProgressCount(UnitTypes.TERRAN_ORBITALCOMMAND))
            {
                BuildOptions.StrictWorkerCount = true;
                MacroData.DesiredUnitCounts[UnitTypes.TERRAN_SCV] = UnitCountService.Count(UnitTypes.TERRAN_SCV);
                var commandCenterBuildingScv = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.TRAIN_SCV));
                if (commandCenterBuildingScv != null)
                {
                    UnitRequestCancellingService.RequestCancel(commandCenterBuildingScv);
                }
            }
        }

        protected void ScanAttackPoint()
        {
            if (MacroData.Minerals >= 50 && MapDataService.LastFrameVisibility(TargetingData.AttackPoint) < MacroData.Frame - (ScanAttackPointTime * SharkyOptions.FramesPerSecond))
            {
                if (OrbitalManager.ScanQueue.Count == 0 && OrbitalManager.LastScanFrame < MacroData.Frame - 10 && !SharkyUnitData.Effects.Any(e => e.EffectId == (uint)Effects.SCAN && e.Alliance == Alliance.Self))
                {
                    OrbitalManager.ScanQueue.Push(TargetingData.AttackPoint);
                }
            }
        }

        protected void ScanNextEnemyBase()
        {
            if (MacroData.Minerals >= 50 && OrbitalManager.ScanQueue.Count == 0 && OrbitalManager.LastScanFrame < MacroData.Frame - 10 && !SharkyUnitData.Effects.Any(e => e.EffectId == (uint)Effects.SCAN && e.Alliance == Alliance.Self))
            {
                var nextEnemyExpansion = BaseData.EnemyBaseLocations.FirstOrDefault(b => !BaseData.EnemyBases.Any(e => b.Location == e.Location));
                if (nextEnemyExpansion != null)
                {
                    if (SharkyOptions != null && MapDataService.LastFrameVisibility(nextEnemyExpansion.Location) < MacroData.Frame - (ScanNextEnemyBaseTime * SharkyOptions.FramesPerSecond))
                    {
                        OrbitalManager.ScanQueue.Push(nextEnemyExpansion.Location);
                    }
                }
            }
        }
    }
}
