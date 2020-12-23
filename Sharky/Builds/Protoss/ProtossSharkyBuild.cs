using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected ChronoData ChronoData;
        protected ICounterTransitioner CounterTransitioner;

        public ProtossSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, unitCountService)
        {
            ChronoData = chronoData;
            CounterTransitioner = counterTransitioner;
        }

        public override List<string> CounterTransition(int frame)
        {
            return CounterTransitioner.DefaultCounterTransition(frame);
        }
    }
}
