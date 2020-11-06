using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public class MacroSetup
    {
        public void SetupMacro(MacroData macroData)
        {
            SetupUnits(macroData);
            SetupProduction(macroData);
            SetupTech(macroData);
            SetupDefensiveBuildings(macroData);
        }

        void SetupUnits(MacroData macroData)
        {
            macroData.Units = new List<UnitTypes>();

            if (macroData.Race == Race.Protoss)
            {
                macroData.NexusUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
                macroData.GatewayUnits = new List<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
                macroData.RoboticsFacilityUnits = new List<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
                macroData.StargateUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

                macroData.Units.AddRange(macroData.NexusUnits);
                macroData.Units.AddRange(macroData.GatewayUnits);
                macroData.Units.AddRange(macroData.RoboticsFacilityUnits);
                macroData.Units.AddRange(macroData.StargateUnits);
                macroData.Units.Add(UnitTypes.PROTOSS_ARCHON);
            }

            macroData.DesiredUnitCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildUnits = new Dictionary<UnitTypes, bool>();
            foreach (var unitType in macroData.Units)
            {
                macroData.DesiredUnitCounts[unitType] = 0;
                macroData.BuildUnits[unitType] = false;
            }
        }

        void SetupProduction(MacroData macroData)
        {
            macroData.DesiredProductionCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildProduction = new Dictionary<UnitTypes, bool>();
            if (macroData.Race == Race.Protoss)
            {
                macroData.Production = new List<UnitTypes> {
                    UnitTypes.PROTOSS_NEXUS, UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_ROBOTICSFACILITY, UnitTypes.PROTOSS_STARGATE
                };
            }

            foreach (var productionType in macroData.Production)
            {
                macroData.DesiredProductionCounts[productionType] = 0;
                macroData.BuildProduction[productionType] = false;
            }

            if (macroData.Race == Race.Protoss)
            {
                macroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
            }
        }

        void SetupTech(MacroData macroData)
        {
            macroData.DesiredTechCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildTech = new Dictionary<UnitTypes, bool>();

            if (macroData.Race == Race.Protoss)
            {
                macroData.Tech = new List<UnitTypes> {
                    UnitTypes.PROTOSS_CYBERNETICSCORE, UnitTypes.PROTOSS_FORGE, UnitTypes.PROTOSS_ROBOTICSBAY, UnitTypes.PROTOSS_TWILIGHTCOUNCIL, UnitTypes.PROTOSS_FLEETBEACON, UnitTypes.PROTOSS_TEMPLARARCHIVE, UnitTypes.PROTOSS_DARKSHRINE
                };
            }

            foreach (var techType in macroData.Tech)
            {
                macroData.DesiredTechCounts[techType] = 0;
                macroData.BuildTech[techType] = false;
            }
        }

        void SetupDefensiveBuildings(MacroData macroData)
        {
            macroData.DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildDefensiveBuildings = new Dictionary<UnitTypes, bool>();

            if (macroData.Race == Race.Protoss)
            {
                macroData.DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.PROTOSS_PHOTONCANNON, UnitTypes.PROTOSS_SHIELDBATTERY
                };
            }

            foreach (var defensiveBuildingsType in macroData.DefensiveBuildings)
            {
                macroData.DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;
                macroData.BuildDefensiveBuildings[defensiveBuildingsType] = false;
            }
        }
    }
}
