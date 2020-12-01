using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected NexusManager NexusManager;
        protected ICounterTransitioner CounterTransitioner;

        public ProtossSharkyBuild(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager, ICounterTransitioner counterTransitioner) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {
            NexusManager = nexusManager;
            CounterTransitioner = counterTransitioner;
        }

        public override List<string> CounterTransition(int frame)
        {
            return CounterTransitioner.DefaultCounterTransition(frame);
        }
    }
}
