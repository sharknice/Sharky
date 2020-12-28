using Sharky.Builds.BuildChoosing;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class EmptyCounterTransitioner : ICounterTransitioner
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        public EmptyCounterTransitioner(EnemyData enemyData, SharkyOptions sharkyOptions)
        {
            EnemyData = enemyData;
            SharkyOptions = sharkyOptions;
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            return null;
        }
    }
}
