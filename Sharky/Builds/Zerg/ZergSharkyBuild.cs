using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : SharkyBuild
    {
        protected TargetingData TargetingData;

        public ZergSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        public ZergSharkyBuild (BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData,
            ChatService chatService, UnitCountService unitCountService,
            FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService, frameToTimeConverter)
        {
        }

        protected void SendDroneForHatchery(int frame)
        {
            if (TargetingData != null && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY) == 1 && MacroData.Minerals > 200)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }
    }
}
