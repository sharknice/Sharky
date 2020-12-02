using SC2APIProtocol;
using Sharky.Builds;
using System.Collections.Generic;

namespace Sharky
{
    public class MacroData
    {
        public Race Race;
        public List<UnitTypes> Units;
        public Dictionary<UnitTypes, int> DesiredUnitCounts;
        public Dictionary<UnitTypes, bool> BuildUnits;

        public List<UnitTypes> Production;
        public Dictionary<UnitTypes, int> DesiredProductionCounts;
        public Dictionary<UnitTypes, bool> BuildProduction;

        public List<UnitTypes> Morphs;
        public Dictionary<UnitTypes, int> DesiredMorphCounts;
        public Dictionary<UnitTypes, bool> Morph;

        public List<UnitTypes> Tech;
        public Dictionary<UnitTypes, int> DesiredTechCounts;
        public Dictionary<UnitTypes, bool> BuildTech;

        public List<UnitTypes> DefensiveBuildings;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, bool> BuildDefensiveBuildings;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsAtDefensivePoint;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsAtEveryBase;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsAtEveryMineralLine;
        public float DefensiveBuildingMaximumDistance { get; set; }
        public float DefensiveBuildingMineralLineMaximumDistance { get; set; }

        public Dictionary<Upgrades, bool> DesiredUpgrades;
        public int DesiredGases;
        public bool BuildGas;

        public List<UnitTypes> AddOns;
        public Dictionary<UnitTypes, int> DesiredAddOnCounts;
        public Dictionary<UnitTypes, bool> BuildAddOns;

        public List<UnitTypes> NexusUnits;
        public List<UnitTypes> GatewayUnits;
        public List<UnitTypes> RoboticsFacilityUnits;
        public List<UnitTypes> StargateUnits;

        public List<UnitTypes> CommandCenterUnits;
        public List<UnitTypes> BarracksUnits;
        public List<UnitTypes> FactoryUnits;
        public List<UnitTypes> StarportUnits;

        public List<UnitTypes> HatcheryUnits;
        public List<UnitTypes> LarvaUnits;

        public int DesiredPylons;
        public bool BuildPylon;
        public int DesiredPylonsAtEveryBase;
        public int DesiredPylonsAtDefensivePoint;
        public int DesiredPylonsAtEveryMineralLine;
        public bool BuildSupplyDepot;
        public int DesiredSupplyDepots;
        public bool BuildOverlord;
        public int DesiredOverlords;

        public Dictionary<string, ProxyData> Proxies { get; set; }

        public int FoodUsed { get; set; }
        public int FoodLeft { get; set; }
        public int FoodArmy { get; set; }
        public int Minerals { get; set; }
        public int VespeneGas { get; set; }
        public int Frame { get; set; }
    }
}
