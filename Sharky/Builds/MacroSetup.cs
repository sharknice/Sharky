using SC2APIProtocol;
using Sharky.Managers;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public class MacroSetup
    {
        public void SetupMacro(MacroManager macroManager, Race race)
        {
            SetupUnits(macroManager, race);
            SetupProduction(macroManager, race);
            SetupTech(macroManager, race);
            SetupDefensiveBuildings(macroManager, race);
        }

        void SetupUnits(MacroManager macroManager, Race race)
        {
            macroManager.Units = new List<UnitTypes>();

            if (race == Race.Protoss)
            {
                macroManager.NexusUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
                macroManager.GatewayUnits = new List<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
                macroManager.RoboticsFacilityUnits = new List<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
                macroManager.StargateUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

                macroManager.Units.AddRange(macroManager.NexusUnits);
                macroManager.Units.AddRange(macroManager.GatewayUnits);
                macroManager.Units.AddRange(macroManager.RoboticsFacilityUnits);
                macroManager.Units.AddRange(macroManager.StargateUnits);
                macroManager.Units.Add(UnitTypes.PROTOSS_ARCHON);
            }

            macroManager.DesiredUnitCounts = new Dictionary<UnitTypes, int>();
            macroManager.BuildUnits = new Dictionary<UnitTypes, bool>();
            foreach (var unitType in macroManager.Units)
            {
                macroManager.DesiredUnitCounts[unitType] = 0;
                macroManager.BuildUnits[unitType] = false;
            }
        }

        void SetupProduction(MacroManager macroManager, Race race)
        {
            macroManager.DesiredProductionCounts = new Dictionary<UnitTypes, int>();
            macroManager.BuildProduction = new Dictionary<UnitTypes, bool>();
            if (race == Race.Protoss)
            {
                macroManager.Production = new List<UnitTypes> {
                    UnitTypes.PROTOSS_NEXUS, UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_ROBOTICSFACILITY, UnitTypes.PROTOSS_STARGATE
                };
            }

            foreach (var productionType in macroManager.Production)
            {
                macroManager.DesiredProductionCounts[productionType] = 0;
                macroManager.BuildProduction[productionType] = false;
            }

            if (race == Race.Protoss)
            {
                macroManager.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
            }
        }

        void SetupTech(MacroManager macroManager, Race race)
        {
            macroManager.DesiredTechCounts = new Dictionary<UnitTypes, int>();
            macroManager.BuildTech = new Dictionary<UnitTypes, bool>();

            if (race == Race.Protoss)
            {
                macroManager.Tech = new List<UnitTypes> {
                    UnitTypes.PROTOSS_CYBERNETICSCORE, UnitTypes.PROTOSS_FORGE, UnitTypes.PROTOSS_ROBOTICSBAY, UnitTypes.PROTOSS_TWILIGHTCOUNCIL, UnitTypes.PROTOSS_FLEETBEACON, UnitTypes.PROTOSS_TEMPLARARCHIVE, UnitTypes.PROTOSS_DARKSHRINE
                };
            }

            foreach (var techType in macroManager.Tech)
            {
                macroManager.DesiredTechCounts[techType] = 0;
                macroManager.BuildTech[techType] = false;
            }
        }

        void SetupDefensiveBuildings(MacroManager macroManager, Race race)
        {
            macroManager.DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, int>();
            macroManager.BuildDefensiveBuildings = new Dictionary<UnitTypes, bool>();

            if (race == Race.Protoss)
            {
                macroManager.DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.PROTOSS_PHOTONCANNON, UnitTypes.PROTOSS_SHIELDBATTERY
                };
            }

            foreach (var defensiveBuildingsType in macroManager.DefensiveBuildings)
            {
                macroManager.DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;
                macroManager.BuildDefensiveBuildings[defensiveBuildingsType] = false;
            }
        }
    }
}
