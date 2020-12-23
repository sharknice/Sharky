using Sharky.Managers;

namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        public TerranSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, unitCountService)
        {
        }
    }
}
