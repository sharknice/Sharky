using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        TargetingData TargetingData;
        MapDataService MapDataService;

        public TerranSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
        }

        public TerranSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData, TargetingData targetingData,
            ChatService chatService, UnitCountService unitCountService, 
            FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService, frameToTimeConverter)
        {
            TargetingData = targetingData;
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
                if (MapDataService != null && MapDataService.MapData.TerranWallData != null)
                {
                    var wallData = MapDataService.MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
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
    }
}
