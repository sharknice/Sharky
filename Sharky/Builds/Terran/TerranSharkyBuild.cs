using Sharky.Chat;

namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        public TerranSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatService, unitCountService)
        {
        }
    }
}
