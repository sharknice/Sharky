namespace Sharky.Builds
{
    public class MacroSetup
    {
        public void SetupMacro(MacroData macroData)
        {
            SetupUnits(macroData);
            SetupProduction(macroData);
            SetupMorphs(macroData);
            SetupTech(macroData);
            SetupAddOns(macroData);
            SetupDefensiveBuildings(macroData);

            macroData.Proxies = new Dictionary<string, ProxyData>();
            macroData.AddOnSwaps = new Dictionary<string, AddOnSwap>();
        }

        void SetupUnits(MacroData macroData)
        {
            macroData.Units = new List<UnitTypes>();

            if (macroData.Race == Race.Protoss)
            {
                macroData.ProtossMacroData.NexusUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
                macroData.ProtossMacroData.GatewayUnits = new List<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
                macroData.ProtossMacroData.RoboticsFacilityUnits = new List<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
                macroData.ProtossMacroData.StargateUnits = new List<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

                macroData.Units.AddRange(macroData.ProtossMacroData.NexusUnits);
                macroData.Units.AddRange(macroData.ProtossMacroData.GatewayUnits);
                macroData.Units.AddRange(macroData.ProtossMacroData.RoboticsFacilityUnits);
                macroData.Units.AddRange(macroData.ProtossMacroData.StargateUnits);
                macroData.Units.Add(UnitTypes.PROTOSS_ARCHON);
            }
            else if (macroData.Race == Race.Terran)
            {
                macroData.CommandCenterUnits = new List<UnitTypes> { UnitTypes.TERRAN_SCV };
                macroData.BarracksUnits = new List<UnitTypes> { UnitTypes.TERRAN_MARINE, UnitTypes.TERRAN_MARAUDER, UnitTypes.TERRAN_REAPER, UnitTypes.TERRAN_GHOST };
                macroData.FactoryUnits = new List<UnitTypes> { UnitTypes.TERRAN_HELLION, UnitTypes.TERRAN_HELLIONTANK, UnitTypes.TERRAN_WIDOWMINE, UnitTypes.TERRAN_CYCLONE, UnitTypes.TERRAN_SIEGETANK, UnitTypes.TERRAN_THOR };
                macroData.StarportUnits = new List<UnitTypes> { UnitTypes.TERRAN_VIKINGFIGHTER, UnitTypes.TERRAN_MEDIVAC, UnitTypes.TERRAN_LIBERATOR, UnitTypes.TERRAN_RAVEN, UnitTypes.TERRAN_BANSHEE, UnitTypes.TERRAN_BATTLECRUISER };

                macroData.Units.AddRange(macroData.CommandCenterUnits);
                macroData.Units.AddRange(macroData.BarracksUnits);
                macroData.Units.AddRange(macroData.FactoryUnits);
                macroData.Units.AddRange(macroData.StarportUnits);
            }
            else
            {
                macroData.HatcheryUnits = new List<UnitTypes> { UnitTypes.ZERG_QUEEN };
                macroData.LarvaUnits = new List<UnitTypes> { UnitTypes.ZERG_OVERLORD, UnitTypes.ZERG_DRONE, UnitTypes.ZERG_ZERGLING, UnitTypes.ZERG_ROACH, UnitTypes.ZERG_HYDRALISK, UnitTypes.ZERG_INFESTOR, UnitTypes.ZERG_HYDRALISK, UnitTypes.ZERG_MUTALISK, UnitTypes.ZERG_CORRUPTOR, UnitTypes.ZERG_ULTRALISK, UnitTypes.ZERG_VIPER, UnitTypes.ZERG_SWARMHOSTMP };

                macroData.Units.AddRange(macroData.HatcheryUnits);
                macroData.Units.AddRange(macroData.LarvaUnits);
                macroData.Units.Add(UnitTypes.ZERG_BANELING);
                macroData.Units.Add(UnitTypes.ZERG_RAVAGER);
                macroData.Units.Add(UnitTypes.ZERG_LURKERMP);
                macroData.Units.Add(UnitTypes.ZERG_BROODLORD);
                macroData.Units.Add(UnitTypes.ZERG_OVERSEER);
                macroData.Units.Add(UnitTypes.ZERG_OVERLORDTRANSPORT);
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
            else if (macroData.Race == Race.Terran)
            {
                macroData.Production = new List<UnitTypes> {
                    UnitTypes.TERRAN_COMMANDCENTER, UnitTypes.TERRAN_BARRACKS, UnitTypes.TERRAN_FACTORY, UnitTypes.TERRAN_STARPORT
                };
            }
            else
            {
                macroData.Production = new List<UnitTypes> {
                    UnitTypes.ZERG_HATCHERY, UnitTypes.ZERG_LARVA, UnitTypes.ZERG_NYDUSNETWORK
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
            else if (macroData.Race == Race.Terran)
            {
                macroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] = 1;
            }
            else
            {
                macroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] = 1;
            }
        }

        void SetupMorphs(MacroData macroData)
        {
            macroData.DesiredMorphCounts = new Dictionary<UnitTypes, int>();
            macroData.Morph = new Dictionary<UnitTypes, bool>();
            if (macroData.Race == Race.Protoss)
            {
                macroData.Morphs = new List<UnitTypes>();
            }
            else if (macroData.Race == Race.Terran)
            {
                macroData.Morphs = new List<UnitTypes> {
                    UnitTypes.TERRAN_ORBITALCOMMAND, UnitTypes.TERRAN_PLANETARYFORTRESS
                };
            }
            else
            {
                macroData.Morphs = new List<UnitTypes> {
                    UnitTypes.ZERG_LAIR, UnitTypes.ZERG_HIVE, UnitTypes.ZERG_GREATERSPIRE
                };
            }

            foreach (var productionType in macroData.Morphs)
            {
                macroData.DesiredMorphCounts[productionType] = 0;
                macroData.Morph[productionType] = false;
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
            else if (macroData.Race == Race.Terran)
            {
                macroData.Tech = new List<UnitTypes> {
                    UnitTypes.TERRAN_ENGINEERINGBAY, UnitTypes.TERRAN_GHOSTACADEMY, UnitTypes.TERRAN_ARMORY, UnitTypes.TERRAN_FUSIONCORE
                };
            }
            else
            {
                macroData.Tech = new List<UnitTypes> {
                    UnitTypes.ZERG_SPAWNINGPOOL, UnitTypes.ZERG_ROACHWARREN, UnitTypes.ZERG_BANELINGNEST, UnitTypes.ZERG_EVOLUTIONCHAMBER, UnitTypes.ZERG_INFESTATIONPIT, UnitTypes.ZERG_HYDRALISKDEN, UnitTypes.ZERG_LURKERDENMP, UnitTypes.ZERG_ULTRALISKCAVERN, UnitTypes.ZERG_SPIRE
                };
            }

            foreach (var techType in macroData.Tech)
            {
                macroData.DesiredTechCounts[techType] = 0;
                macroData.BuildTech[techType] = false;
            }
        }

        void SetupAddOns(MacroData macroData)
        {
            macroData.DesiredAddOnCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildAddOns = new Dictionary<UnitTypes, bool>();

            if (macroData.Race == Race.Terran)
            {
                macroData.AddOns = new List<UnitTypes> {
                    UnitTypes.TERRAN_BARRACKSREACTOR, UnitTypes.TERRAN_BARRACKSTECHLAB, UnitTypes.TERRAN_FACTORYREACTOR, UnitTypes.TERRAN_FACTORYTECHLAB, UnitTypes.TERRAN_STARPORTREACTOR, UnitTypes.TERRAN_STARPORTTECHLAB
                };
            }
            else
            {
                macroData.AddOns = new List<UnitTypes>();
            }

            foreach (var techType in macroData.AddOns)
            {
                macroData.DesiredAddOnCounts[techType] = 0;
                macroData.BuildAddOns[techType] = false;
            }
        }

        void SetupDefensiveBuildings(MacroData macroData)
        {
            macroData.DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, int>();
            macroData.BuildDefensiveBuildings = new Dictionary<UnitTypes, bool>();
            macroData.DesiredDefensiveBuildingsAtDefensivePoint = new Dictionary<UnitTypes, int>();
            macroData.DesiredDefensiveBuildingsAtEveryBase = new Dictionary<UnitTypes, int>();
            macroData.DesiredDefensiveBuildingsAtNextBase = new Dictionary<UnitTypes, int>();
            macroData.DesiredDefensiveBuildingsAtEveryMineralLine = new Dictionary<UnitTypes, int>();
            macroData.DefensiveBuildingMaximumDistance = 15;
            macroData.DefensiveBuildingMineralLineMaximumDistance = 6;

            if (macroData.Race == Race.Protoss)
            {
                macroData.DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.PROTOSS_PHOTONCANNON, UnitTypes.PROTOSS_SHIELDBATTERY
                };
            }
            else if (macroData.Race == Race.Terran)
            {
                macroData.DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.TERRAN_MISSILETURRET, UnitTypes.TERRAN_BUNKER, UnitTypes.TERRAN_SENSORTOWER
                };
            }
            else
            {
                macroData.DefensiveBuildings = new List<UnitTypes> {
                    UnitTypes.ZERG_SPINECRAWLER, UnitTypes.ZERG_SPORECRAWLER
                };
            }

            foreach (var defensiveBuildingsType in macroData.DefensiveBuildings)
            {
                macroData.DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;             
                macroData.BuildDefensiveBuildings[defensiveBuildingsType] = false;
                macroData.DesiredDefensiveBuildingsAtDefensivePoint[defensiveBuildingsType] = 0;
                macroData.DesiredDefensiveBuildingsAtEveryBase[defensiveBuildingsType] = 0;
                macroData.DesiredDefensiveBuildingsAtEveryMineralLine[defensiveBuildingsType] = 0;
                macroData.DesiredDefensiveBuildingsAtNextBase[defensiveBuildingsType] = 0;
            }
        }
    }
}
