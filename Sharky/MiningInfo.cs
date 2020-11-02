using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky
{
    public class MiningInfo
    {
        public MiningInfo(Unit resourceUnit)
        {
            ResourceUnit = resourceUnit;
            Workers = new List<UnitCommander>();
        }

        public List<UnitCommander> Workers { get; set; }
        public Unit ResourceUnit { get; set; }
    }
}
