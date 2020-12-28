using Sharky;
using Sharky.Builds.BuildChoosing;
using System.Collections.Generic;

namespace SharkyExampleBot
{
    public class ProtossCounterTransitioner : ICounterTransitioner
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        public ProtossCounterTransitioner(EnemyData enemyData, SharkyOptions sharkyOptions)
        {
            EnemyData = enemyData;
            SharkyOptions = sharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            if (EnemyData.EnemyStrategies["ZerglingRush"].Active && (frame < SharkyOptions.FramesPerSecond * 5 * 60))
            {
                return new List<string> { "ZealotRush" };
            }

            return null;
        }
    }
}
