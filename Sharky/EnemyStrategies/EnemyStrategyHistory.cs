using System.Collections.Generic;

namespace Sharky.EnemyStrategies
{
    public class EnemyStrategyHistory
    {
        public Dictionary<int, string> History { get; set; }

        public EnemyStrategyHistory()
        {
            History = new Dictionary<int, string>();
        }
    }
}
