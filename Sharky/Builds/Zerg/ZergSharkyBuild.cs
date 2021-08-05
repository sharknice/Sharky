using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : SharkyBuild
    {
        public ZergSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
        }

        public ZergSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData,
            ChatService chatService, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService)
        {
        }
    }
}
