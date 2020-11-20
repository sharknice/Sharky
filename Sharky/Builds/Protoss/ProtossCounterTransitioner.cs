using Sharky.Managers;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class ProtossCounterTransitioner
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
            if (EnemyStrategyManager.EnemyStrategies["MarineRush"].Active && (frame < SharkyOptions.FramesPerSecond * 5 * 60))
            {
                return new List<string> { "AntiMassMarine", "NexusFirst", "Robo", "ProtossRobo" };
            }

            return null;
        }
    }
}
