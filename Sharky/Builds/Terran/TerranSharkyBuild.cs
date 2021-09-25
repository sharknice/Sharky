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
                if (MapDataService != null)
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
    }
}
