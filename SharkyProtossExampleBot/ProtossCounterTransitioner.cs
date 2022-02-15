using Sharky;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace SharkyProtossExampleBot
{
    public class ProtossCounterTransitioner : ICounterTransitioner
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        public ProtossCounterTransitioner(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            if (EnemyData.EnemyStrategies["ZerglingRush"].Active && (frame < SharkyOptions.FramesPerSecond * 3 * 60))
            {
                return new List<string> { "ZealotRush" };
            }

            return null;
        }
    }
}
