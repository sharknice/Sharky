using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class EnemyStrategyManager : SharkyManager
    {
        EnemyData EnemyData;

        public EnemyStrategyManager(EnemyData enemyData)
        {
            EnemyData = enemyData;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            foreach (var enemyStrategy in EnemyData.EnemyStrategies.Values)
            {
                enemyStrategy.OnFrame(frame);
            }

            return new List<Action>();
        }
    }
}
