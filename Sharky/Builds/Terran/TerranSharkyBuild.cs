using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        public TerranSharkyBuild(DefaultSharkyBot defaultSharkyBot)
            : base(defaultSharkyBot)
        {
        }

        public TerranSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData, 
            ChatService chatService, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService)
        {
        }
    }
}
