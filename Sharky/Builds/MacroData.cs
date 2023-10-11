using Sharky.Helper;

namespace Sharky
{
    public class MacroData
    {
        public Race Race;
        public List<UnitTypes> Units;
        public Dictionary<UnitTypes, ValueRange> DesiredUnitCounts;
        public Dictionary<UnitTypes, bool> BuildUnits;

        public List<UnitTypes> Production;
        public Dictionary<UnitTypes, ValueRange> DesiredProductionCounts;
        public Dictionary<UnitTypes, bool> BuildProduction;

        public List<UnitTypes> Morphs;
        public Dictionary<UnitTypes, ValueRange> DesiredMorphCounts;
        public Dictionary<UnitTypes, bool> Morph;

        public List<UnitTypes> Tech;
        public Dictionary<UnitTypes, ValueRange> DesiredTechCounts;
        public Dictionary<UnitTypes, bool> BuildTech;

        public List<UnitTypes> DefensiveBuildings;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, bool> BuildDefensiveBuildings;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsAtDefensivePoint;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsAtEveryBase;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsAtNextBase;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsAtEveryMineralLine;
        public float DefensiveBuildingMaximumDistance { get; set; }
        public float DefensiveBuildingMineralLineMaximumDistance { get; set; }

        public Dictionary<Upgrades, bool> DesiredUpgrades;
        public ValueRange DesiredGases;
        public bool BuildGas;

        public List<UnitTypes> AddOns;
        public Dictionary<UnitTypes, ValueRange> DesiredAddOnCounts;
        public Dictionary<UnitTypes, bool> BuildAddOns;

        public List<UnitTypes> CommandCenterUnits;
        public List<UnitTypes> BarracksUnits;
        public List<UnitTypes> FactoryUnits;
        public List<UnitTypes> StarportUnits;

        public List<UnitTypes> HatcheryUnits;
        public List<UnitTypes> LarvaUnits;

        public bool BuildSupplyDepot;
        public ValueRange DesiredSupplyDepots;
        public bool BuildOverlord;
        public ValueRange DesiredOverlords;
        public ValueRange DesiredMacroCommandCenters;

        public ProtossMacroData ProtossMacroData { get; set; } = new ProtossMacroData();

        public Dictionary<string, ProxyData> Proxies { get; set; }
        public Dictionary<string, AddOnSwap> AddOnSwaps { get; set; }

        public int FoodUsed { get; set; }
        public int FoodLeft { get; set; }
        public int FoodArmy { get; set; }
        public int FoodWorkers { get; set; }
        public int Minerals { get; set; }
        public int VespeneGas { get; set; }
        public int Frame { get; set; }


    }
}
