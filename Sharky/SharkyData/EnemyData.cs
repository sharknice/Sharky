
using SC2APIProtocol;
using Sharky.EnemyStrategies;
using System.Collections.Generic;

namespace Sharky
{
    public class EnemyData
    {
        public Race EnemyRace { get; set; }
        public Dictionary<string, IEnemyStrategy> EnemyStrategies { get; set; }
        public Race SelfRace { get; set; }
        public EnemyPlayer.EnemyPlayer EnemyPlayer { get; set; }
        public EnemyAggressivityData EnemyAggressivityData { get; set; }
    }
}
