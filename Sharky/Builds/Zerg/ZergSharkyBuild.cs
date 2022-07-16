using SC2APIProtocol;
using Sharky.Builds.QuickBuilds;
using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : QuickBuild
    {
        protected TargetingData TargetingData;

        public ZergSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected void SendDroneForHatchery(int frame)
        {
            if (TargetingData != null && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY) == 1 && MacroData.Minerals > 180)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
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
        }
    }
}
