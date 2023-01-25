using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky
{
    public class ActiveUnitData
    {
        public ActiveUnitData()
        {
            EnemyUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            SelfUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            NeutralUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            Commanders = new ConcurrentDictionary<ulong, UnitCommander>();
            DeadUnits = new List<ulong>();
            EnemyDeaths = 0;
            SelfDeaths = 0;
            NeutralDeaths = 0;
            SelfResourcesLost = 0;
            EnemyResourcesLost = 0;
        }

        public ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCalculation> SelfUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCommander> Commanders { get; set; }
        public List<ulong> DeadUnits { get; set; }

        public int EnemyDeaths { get; set; }
        public int SelfDeaths { get; set; }
        public int NeutralDeaths { get; set; }

        public int SelfResourcesLost { get; set; }
        public int EnemyResourcesLost { get; set; }
    }
}
