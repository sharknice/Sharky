using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class EmptyCounterTransitioner : ICounterTransitioner
    {
        EnemyStrategyManager EnemyStrategyManager;
        SharkyOptions SharkyOptions;

        public EmptyCounterTransitioner(EnemyStrategyManager enemyStrategyManager, SharkyOptions sharkyOptions)
        {
            EnemyStrategyManager = enemyStrategyManager;
            SharkyOptions = sharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            return null;
        }
    }
}
