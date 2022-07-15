using System.Collections.Generic;

namespace Sharky.Builds
{
    public class UnitTypeBuildClassifications
    {
        public HashSet<UnitTypes> ProducedUnits { get; private set; }

        public HashSet<UnitTypes> ProtossProducedUnits { get; private set; }
        public HashSet<UnitTypes> NexusUnits { get; private set; }
        public HashSet<UnitTypes> GatewayUnits { get; private set; }
        public HashSet<UnitTypes> RoboticsFacilityUnits { get; private set; }
        public HashSet<UnitTypes> StargateUnits { get; private set; }

        public HashSet<UnitTypes> TerranProducedUnits { get; private set; }
        public HashSet<UnitTypes> CommandCenterUnits { get; private set; }
        public HashSet<UnitTypes> BarracksUnits { get; private set; }
        public HashSet<UnitTypes> FactoryUnits { get; private set; }
        public HashSet<UnitTypes> StarportUnits { get; private set; }

        public HashSet<UnitTypes> ZergProducedUnits { get; private set; }
        public HashSet<UnitTypes> HatcheryUnits { get; private set; }
        public HashSet<UnitTypes> LarvaUnits { get; private set; }


        public HashSet<UnitTypes> ProductionUnits { get; private set; }
        public HashSet<UnitTypes> ProtossProductionUnits { get; private set; }
        public HashSet<UnitTypes> TerranProductionUnits { get; private set; }
        public HashSet<UnitTypes> ZergProductionUnits { get; private set; }

        public HashSet<UnitTypes> MorphUnits { get; private set; }
        public HashSet<UnitTypes> TerranMorphUnits { get; private set; }
        public HashSet<UnitTypes> ZergMorphUnits { get; private set; }

        public HashSet<UnitTypes> TechUnits { get; private set; }
        public HashSet<UnitTypes> ProtossTechUnits { get; private set; }
        public HashSet<UnitTypes> TerranTechUnits { get; private set; }
        public HashSet<UnitTypes> ZergTechUnits { get; private set; }

        public UnitTypeBuildClassifications()
        {
            SetupProducedUnits();
            SetupProductionUnits();
            SetupMorphs();
            SetupTech();
        }

        private void SetupProducedUnits()
        {
            SetupProtossProducedUnits();
            SetupTerranProducedUnits();
            SetupZergProducedUnits();
            ProducedUnits = new HashSet<UnitTypes>();
            ProducedUnits.UnionWith(ProtossProducedUnits);
            ProducedUnits.UnionWith(TerranProducedUnits);
            ProducedUnits.UnionWith(ZergProducedUnits);
        }

        private void SetupZergProducedUnits()
        {
            ZergProducedUnits = new HashSet<UnitTypes>();
            HatcheryUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_QUEEN };
            LarvaUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_OVERLORD, UnitTypes.ZERG_DRONE, UnitTypes.ZERG_ZERGLING, UnitTypes.ZERG_ROACH, UnitTypes.ZERG_HYDRALISK, UnitTypes.ZERG_INFESTOR, UnitTypes.ZERG_HYDRALISK, UnitTypes.ZERG_MUTALISK, UnitTypes.ZERG_CORRUPTOR, UnitTypes.ZERG_ULTRALISK, UnitTypes.ZERG_VIPER, UnitTypes.ZERG_SWARMHOSTMP };

            ZergProducedUnits.UnionWith(HatcheryUnits);
            ZergProducedUnits.UnionWith(LarvaUnits);
            ZergProducedUnits.Add(UnitTypes.ZERG_BANELING);
            ZergProducedUnits.Add(UnitTypes.ZERG_RAVAGER);
            ZergProducedUnits.Add(UnitTypes.ZERG_LURKERMP);
            ZergProducedUnits.Add(UnitTypes.ZERG_BROODLORD);
            ZergProducedUnits.Add(UnitTypes.ZERG_OVERSEER);
            ZergProducedUnits.Add(UnitTypes.ZERG_OVERLORDTRANSPORT);
        }

        private void SetupTerranProducedUnits()
        {
            TerranProducedUnits = new HashSet<UnitTypes>();
            CommandCenterUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_SCV };
            BarracksUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_MARINE, UnitTypes.TERRAN_MARAUDER, UnitTypes.TERRAN_REAPER, UnitTypes.TERRAN_GHOST };
            FactoryUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_HELLION, UnitTypes.TERRAN_HELLIONTANK, UnitTypes.TERRAN_WIDOWMINE, UnitTypes.TERRAN_CYCLONE, UnitTypes.TERRAN_SIEGETANK, UnitTypes.TERRAN_THOR };
            StarportUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_VIKINGFIGHTER, UnitTypes.TERRAN_MEDIVAC, UnitTypes.TERRAN_LIBERATOR, UnitTypes.TERRAN_RAVEN, UnitTypes.TERRAN_BANSHEE, UnitTypes.TERRAN_BATTLECRUISER };

            TerranProducedUnits.UnionWith(CommandCenterUnits);
            TerranProducedUnits.UnionWith(BarracksUnits);
            TerranProducedUnits.UnionWith(FactoryUnits);
            TerranProducedUnits.UnionWith(StarportUnits);
        }

        private void SetupProtossProducedUnits()
        {
            NexusUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_PROBE, UnitTypes.PROTOSS_MOTHERSHIP };
            GatewayUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ZEALOT, UnitTypes.PROTOSS_STALKER, UnitTypes.PROTOSS_SENTRY, UnitTypes.PROTOSS_ADEPT, UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR };
            RoboticsFacilityUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_OBSERVER, UnitTypes.PROTOSS_IMMORTAL, UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_COLOSSUS, UnitTypes.PROTOSS_DISRUPTOR };
            StargateUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_PHOENIX, UnitTypes.PROTOSS_ORACLE, UnitTypes.PROTOSS_VOIDRAY, UnitTypes.PROTOSS_TEMPEST, UnitTypes.PROTOSS_CARRIER };

            ProtossProducedUnits = new HashSet<UnitTypes>();
            ProtossProducedUnits.UnionWith(NexusUnits);
            ProtossProducedUnits.UnionWith(GatewayUnits);
            ProtossProducedUnits.UnionWith(RoboticsFacilityUnits);
            ProtossProducedUnits.UnionWith(StargateUnits);
            ProtossProducedUnits.Add(UnitTypes.PROTOSS_ARCHON);
        }

        void SetupProductionUnits()
        {
            ProtossProductionUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_NEXUS, UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_ROBOTICSFACILITY, UnitTypes.PROTOSS_STARGATE };
            TerranProducedUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_COMMANDCENTER, UnitTypes.TERRAN_BARRACKS, UnitTypes.TERRAN_FACTORY, UnitTypes.TERRAN_STARPORT };
            ZergProducedUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HATCHERY, UnitTypes.ZERG_LARVA, UnitTypes.ZERG_NYDUSNETWORK };

            ProductionUnits = new HashSet<UnitTypes>();
            ProductionUnits.UnionWith(ProtossProductionUnits);
            ProductionUnits.UnionWith(TerranProducedUnits);
            ProductionUnits.UnionWith(ZergProducedUnits);
        }

        void SetupMorphs()
        {
            TerranMorphUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ORBITALCOMMAND, UnitTypes.TERRAN_PLANETARYFORTRESS };
            ZergMorphUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LAIR, UnitTypes.ZERG_HIVE, UnitTypes.ZERG_GREATERSPIRE };

            MorphUnits = new HashSet<UnitTypes>();
            MorphUnits.UnionWith(TerranMorphUnits);
            MorphUnits.UnionWith(ZergMorphUnits);
        }

        void SetupTech()
        {
            ProtossTechUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE, UnitTypes.PROTOSS_FORGE, UnitTypes.PROTOSS_ROBOTICSBAY, UnitTypes.PROTOSS_TWILIGHTCOUNCIL, UnitTypes.PROTOSS_FLEETBEACON, UnitTypes.PROTOSS_TEMPLARARCHIVE, UnitTypes.PROTOSS_DARKSHRINE };
            TerranTechUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY, UnitTypes.TERRAN_GHOSTACADEMY, UnitTypes.TERRAN_ARMORY, UnitTypes.TERRAN_FUSIONCORE };
            ZergTechUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPAWNINGPOOL, UnitTypes.ZERG_ROACHWARREN, UnitTypes.ZERG_BANELINGNEST, UnitTypes.ZERG_EVOLUTIONCHAMBER, UnitTypes.ZERG_INFESTATIONPIT, UnitTypes.ZERG_HYDRALISKDEN, UnitTypes.ZERG_LURKERDENMP, UnitTypes.ZERG_ULTRALISKCAVERN, UnitTypes.ZERG_SPIRE };

            TechUnits = new HashSet<UnitTypes>();
            TechUnits.UnionWith(ProtossTechUnits);
            TechUnits.UnionWith(TerranTechUnits);
            TechUnits.UnionWith(ZergTechUnits);
        }
    }
}
