using Sharky.Managers;
using Sharky.Managers.Protoss;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected NexusManager NexusManager;

        public ProtossSharkyBuild(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, NexusManager nexusManager) : base(buildOptions, macroData, unitManager, attackData)
        {
            NexusManager = nexusManager;
        }
    }
}
