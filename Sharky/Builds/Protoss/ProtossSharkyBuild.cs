using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected ChronoData ChronoData;
        protected ICounterTransitioner CounterTransitioner;

        public ProtossSharkyBuild(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot)
        {
            ChronoData = defaultSharkyBot.ChronoData;
            CounterTransitioner = counterTransitioner;
        }

        public ProtossSharkyBuild(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService, MicroTaskData microTaskData, FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService, frameToTimeConverter)
        {
            ChronoData = chronoData;
            CounterTransitioner = counterTransitioner;
        }

        public override List<string> CounterTransition(int frame)
        {
            return CounterTransitioner.DefaultCounterTransition(frame);
        }

        public override void StartBuild(int frame)
        {  
            base.StartBuild(frame);

            BuildOptions.WallOffType = BuildingPlacement.WallOffType.Partial;
        }
    }
}
