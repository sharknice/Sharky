using Sharky.Managers;

namespace Sharky.Builds
{
    public class BuildNothing : SharkyBuild
    {
        public BuildNothing(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, unitCountService)
        {

        }
    }
}
