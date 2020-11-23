using Sharky.Managers;

namespace Sharky.Builds.Terran
{
    public class TerranSharkyBuild : SharkyBuild
    {
        public TerranSharkyBuild(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {
        }
    }
}
