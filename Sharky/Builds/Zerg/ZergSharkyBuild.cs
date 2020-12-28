using Sharky.Chat;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : SharkyBuild
    {
        public ZergSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, unitCountService)
        {
        }
    }
}
