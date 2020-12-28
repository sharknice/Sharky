using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected ChronoData ChronoData;
        protected ICounterTransitioner CounterTransitioner;

        public ProtossSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, unitCountService)
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
