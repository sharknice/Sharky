using Sharky;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using System.Collections.Generic;

namespace SharkyExampleBot
{
    public class ProtossCounterTransitioner : ICounterTransitioner
    {
        EnemyStrategyManager EnemyStrategyManager;
        SharkyOptions SharkyOptions;

        public ProtossCounterTransitioner(EnemyStrategyManager enemyStrategyManager, SharkyOptions sharkyOptions)
        {
            EnemyStrategyManager = enemyStrategyManager;
            SharkyOptions = sharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            if (EnemyStrategyManager.EnemyStrategies["ZerglingRush"].Active && (frame < SharkyOptions.FramesPerSecond * 5 * 60))
            {
                return new List<string> { "ZealotRush" };
            }

            return null;
        }
    }
}
