using SC2APIProtocol;
using Sharky.EnemyStrategies;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class EnemyStrategyManager : SharkyManager
    {
        public Dictionary<string, IEnemyStrategy> EnemyStrategies { get; private set; }

        public EnemyStrategyManager(Dictionary<string, IEnemyStrategy> enemyStrategies)
        {
            EnemyStrategies = enemyStrategies;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            foreach (var enemyStrategy in EnemyStrategies.Values)
            {
                enemyStrategy.OnFrame(frame);
            }

            return new List<Action>();
        }
    }
}
