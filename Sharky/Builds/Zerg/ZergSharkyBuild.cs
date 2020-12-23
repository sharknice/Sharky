using Sharky.Managers;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : SharkyBuild
    {
        public ZergSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, unitCountService)
        {
        }
    }
}
