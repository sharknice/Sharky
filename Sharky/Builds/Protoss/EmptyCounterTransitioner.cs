using Sharky.Builds.BuildChoosing;
using Sharky.EnemyStrategies;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class EmptyCounterTransitioner : ICounterTransitioner
    {
        Dictionary<string, IEnemyStrategy> EnemyStrategies;
        SharkyOptions SharkyOptions;

        public EmptyCounterTransitioner(Dictionary<string, IEnemyStrategy> enemyStrategies, SharkyOptions sharkyOptions)
        {
            EnemyStrategies = enemyStrategies;
            SharkyOptions = sharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            return null;
        }
    }
}
