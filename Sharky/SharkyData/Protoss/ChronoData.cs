using System.Collections.Generic;

namespace Sharky
{
    public class ChronoData
    {
        public HashSet<UnitTypes> ChronodUnits { get; set; }
        public HashSet<Upgrades> ChronodUpgrades { get; set; }

        public ChronoData()
        {
            ChronodUnits = new HashSet<UnitTypes>();
            ChronodUpgrades = new HashSet<Upgrades>();
        }
    }
}
