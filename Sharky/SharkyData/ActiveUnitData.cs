using System.Collections.Generic;

namespace Sharky
{
    public class ActiveUnitData
    {
        public ActiveUnitData()
        {
            EnemyUnits = new Dictionary<ulong, UnitCalculation>();
            SelfUnits = new Dictionary<ulong, UnitCalculation>();
            NeutralUnits = new Dictionary<ulong, UnitCalculation>();
            Commanders = new Dictionary<ulong, UnitCommander>();
            DeadUnits = new List<ulong>();
            EnemyDeaths = 0;
            SelfDeaths = 0;
            NeutralDeaths = 0;
            SelfResourcesLost = 0;
            EnemyResourcesLost = 0;
        }

        public Dictionary<ulong, UnitCalculation> EnemyUnits { get; set; }
        public Dictionary<ulong, UnitCalculation> SelfUnits { get; set; }
        public Dictionary<ulong, UnitCalculation> NeutralUnits { get; set; }
        public Dictionary<ulong, UnitCommander> Commanders { get; set; }
        public List<ulong> DeadUnits { get; set; }

        public int EnemyDeaths { get; set; }
        public int SelfDeaths { get; set; }
        public int NeutralDeaths { get; set; }

        public int SelfResourcesLost { get; set; }
        public int EnemyResourcesLost { get; set; }
    }
}
