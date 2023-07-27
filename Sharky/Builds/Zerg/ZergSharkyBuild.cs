namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : QuickBuild
    {
        protected TargetingData TargetingData;
        protected BaseData BaseData;
        protected EnemyData EnemyData;

        public ZergSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
        }

        protected void SendDroneForHatchery(int frame, bool natural = true)
        {
            if (natural && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY) == 1 && MacroData.Minerals > 180)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
            else if (!natural && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY) == 2 && UnitCountService.BuildingsInProgressCount(UnitTypes.ZERG_HATCHERY) == 0 && MacroData.Minerals > 100 && !EnemyData.EnemyStrategies[nameof(OneBase)].Active)
            {
                PrePositionBuilderTask.SendBuilder(BaseData.BaseLocations.Skip(2).FirstOrDefault().Location, frame);
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            base.OnFrame(observation);

            if (BuildOptions.ZergBuildOptions.PrepositionDroneForNaturalHatch && QuickBuildOrders != null)
            {
                // Preposition drone for hatchery
                if (QuickBuildOrders.CurrentStep.HasValue
                    && QuickBuildOrders.CurrentStep.Value.Item2 is UnitTypes unitType
                    && unitType == UnitTypes.ZERG_HATCHERY
                    && MacroData.FoodUsed >= QuickBuildOrders.CurrentStep.Value.Item1
                    && UnitCountService.BuildingsDoneAndInProgressCount(UnitTypes.ZERG_HATCHERY) < 2)
                {
                    SendDroneForHatchery((int)observation.Observation.GameLoop);
                }
            }

            if ((QuickBuildOrders?.IsFinished ?? true) && BuildOptions.ZergBuildOptions.PrepositionDroneForThirdHatch)
            {
                SendDroneForHatchery((int)observation.Observation.GameLoop, false);
            }
        }
    }
}
