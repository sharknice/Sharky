using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class MacroManager : SharkyManager
    {
        public List<UnitTypes> Units;
        public Dictionary<UnitTypes, int> DesiredUnitCounts;
        public Dictionary<UnitTypes, bool> BuildUnits;

        public List<UnitTypes> Production;
        public Dictionary<UnitTypes, int> DesiredProductionCounts;

        public Dictionary<UnitTypes, bool> BuildProduction;

        public List<UnitTypes> Tech;
        public Dictionary<UnitTypes, int> DesiredTechCounts;
        public Dictionary<UnitTypes, bool> BuildTech;

        public List<UnitTypes> DefensiveBuildings;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, bool> BuildDefensiveBuildings;

        public Dictionary<Upgrades, bool> DesiredUpgrades;
        public int DesiredGases;
        public bool BuildGas;

        public List<UnitTypes> NexusUnits;
        public List<UnitTypes> GatewayUnits;
        public List<UnitTypes> RoboticsFacilityUnits;
        public List<UnitTypes> StargateUnits;

        public List<UnitTypes> BarracksUnits;
        public List<UnitTypes> FactoryUnits;
        public List<UnitTypes> StarportUnits;

        public int DesiredPylons;
        public bool BuildPylon;

        public Race Race;

        public int FoodUsed { get; private set; }
        public int Minerals { get; private set; }
        public int VespeneGas { get; private set; }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    Race = playerInfo.RaceActual;
                }
            }

            SetupUnits(Race);
            SetupProduction(Race);
            SetupTech(Race);
            SetupDefensiveBuildings(Race);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            FoodUsed = (int)observation.Observation.PlayerCommon.FoodUsed;
            Minerals = (int)observation.Observation.PlayerCommon.Minerals;
            VespeneGas = (int)observation.Observation.PlayerCommon.Vespene;

            return new List<SC2APIProtocol.Action>();
        }

        void SetupUnits(Race race)
        {
            Units = new List<UnitTypes>();

            if (race == Race.Protoss)
            {
                NexusUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
                GatewayUnits = new List<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
                RoboticsFacilityUnits = new List<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
                StargateUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

                Units.AddRange(NexusUnits);
                Units.AddRange(GatewayUnits);
                Units.AddRange(RoboticsFacilityUnits);
                Units.AddRange(StargateUnits);
                Units.Add(UnitTypes.PROTOSS_ARCHON);
            }

            DesiredUnitCounts = new Dictionary<UnitTypes, int>();
            BuildUnits = new Dictionary<UnitTypes, bool>();
            foreach (var unitType in Units)
            {
                DesiredUnitCounts[unitType] = 0;
                BuildUnits[unitType] = false;
            }
        }

        void SetupProduction(Race race)
        {
            DesiredProductionCounts = new Dictionary<UnitTypes, int>();
            BuildProduction = new Dictionary<UnitTypes, bool>();
            if (race == Race.Protoss)
            {
                Production = new List<UnitTypes> {
                    UnitTypes.PROTOSS_NEXUS, UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_ROBOTICSFACILITY, UnitTypes.PROTOSS_STARGATE
                };
            }

            foreach (var productionType in Production)
            {
                DesiredProductionCounts[productionType] = 0;
                BuildProduction[productionType] = false;
            }

            if (race == Race.Protoss)
            {
                DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
            }
        }

        void SetupTech(Race race)
        {
            DesiredTechCounts = new Dictionary<UnitTypes, int>();
            BuildTech = new Dictionary<UnitTypes, bool>();

            if (race == Race.Protoss)
            {
                Tech = new List<UnitTypes> {
                    UnitTypes.PROTOSS_CYBERNETICSCORE, UnitTypes.PROTOSS_FORGE, UnitTypes.PROTOSS_ROBOTICSBAY, UnitTypes.PROTOSS_TWILIGHTCOUNCIL, UnitTypes.PROTOSS_FLEETBEACON, UnitTypes.PROTOSS_TEMPLARARCHIVE, UnitTypes.PROTOSS_DARKSHRINE
                };
            }

            foreach (var techType in Tech)
            {
                DesiredTechCounts[techType] = 0;
                BuildTech[techType] = false;
            }
        }

        void SetupDefensiveBuildings(Race race)
        {
            DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, int>();
            BuildDefensiveBuildings = new Dictionary<UnitTypes, bool>();

            if (race == Race.Protoss)
            {
                DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.PROTOSS_PHOTONCANNON, UnitTypes.PROTOSS_SHIELDBATTERY
                };
            }

            foreach (var defensiveBuildingsType in DefensiveBuildings)
            {
                DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;
                BuildDefensiveBuildings[defensiveBuildingsType] = false;
            }
        }
    }
}
