using Sharky.Managers;

namespace Sharky.Builds.Zerg
{
    public class ZergSharkyBuild : SharkyBuild
    {
        public ZergSharkyBuild(BuildOptions buildOptions, MacroData macroData, IUnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {
        }
    }
}
