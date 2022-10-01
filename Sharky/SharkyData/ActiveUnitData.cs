using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        }

        public ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCalculation> SelfUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits { get; set; }
        public ConcurrentDictionary<ulong, UnitCommander> Commanders { get; set; }
        public List<ulong> DeadUnits { get; set; }

        /// <summary>
        /// Gets highest finished status of units of given type. Useful when you want to know when your tech building finishes.
        /// </summary>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public float TechBuildProgress(UnitTypes unitType) => SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)unitType).Select(u => u.Unit.BuildProgress).DefaultIfEmpty().Max();

        public int EnemyDeaths { get; set; }
        public int SelfDeaths { get; set; }
        public int NeutralDeaths { get; set; }
    }
}
