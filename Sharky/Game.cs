using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky
{
    public class Game
    {
        /// <summary>
        /// when the game ended
        /// </summary>
        public DateTime DateTime { get; set; }
        public string EnemyId { get; set; }
        public string MapName { get; set; }

        public Race EnemySelectedRace { get; set; }
        public Race EnemyRace { get; set; }
        
        public Race MySelectedRace { get; set; }

        /// <summary>
        /// The actual race, terran, protoss, or zerg, not random
        /// </summary>
        public Race MyRace { get; set; }

        /// <summary>
        /// how many frames the game lasted
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 1 win, 2 loss, 3 tie
        /// </summary>
        public int Result { get; set; }

        /// <summary>
        /// Enemy Strategies and the frame they were detected
        /// </summary>
        public Dictionary<int, string> EnemyStrategies { get; set; }

        /// <summary>
        /// Sharkbot builds and the frame they were started
        /// </summary>
        public Dictionary<int, string> Builds { get; set; }

        public Dictionary<int, string> EnemyChat { get; set; }
        public Dictionary<int, string> MyChat { get; set; }
    }
}
