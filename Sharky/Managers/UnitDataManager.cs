using Google.Protobuf.Collections;
using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class UnitDataManager : SharkyManager
    {
        public Dictionary<UnitTypes, UnitTypeData> UnitData { get; private set; }
        public Dictionary<UnitTypes, BuildingTypeData> BuildingData { get; private set; }
        public Dictionary<UnitTypes, TrainingTypeData> TrainingData { get; private set; }
        public Dictionary<Upgrades, TrainingTypeData> UpgradeData { get; private set; }

        /// <summary>
        /// key is ability, value is the unitType it belongs to
        /// </summary>
        Dictionary<Abilities, UnitTypes> UnitAbilities;
        public RepeatedField<uint> ResearchedUpgrades;

        public HashSet<UnitTypes> ZergTypes { get; private set; }
        public HashSet<UnitTypes> ProtossTypes { get; private set; }
        public HashSet<UnitTypes> TerranTypes { get; private set; }

        public HashSet<UnitTypes> MineralFieldTypes { get; private set; }
        public HashSet<UnitTypes> GasGeyserTypes { get; private set; }
        public HashSet<UnitTypes> GasGeyserRefineryTypes { get; private set; }

        public HashSet<UnitTypes> GroundSplashDamagers { get; private set; }
        public HashSet<UnitTypes> AirSplashDamagers { get; private set; }
        public HashSet<UnitTypes> CloakableAttackers { get; private set; }
        public HashSet<UnitTypes> DetectionTypes { get; private set; }
        public HashSet<UnitTypes> AbilityDetectionTypes { get; private set; }
        public HashSet<UnitTypes> NoWeaponCooldownTypes { get; private set; }

        public HashSet<UnitTypes> ResourceCenterTypes { get; private set; }

        public Dictionary<Abilities, float> AbilityCooldownTimes { get; private set; }
        public Dictionary<Abilities, float> WarpInCooldownTimes { get; private set; }

        public HashSet<Buffs> CarryingResourceBuffs { get; private set; }

        public HashSet<Abilities> MiningAbilities { get; private set; }
        public HashSet<Abilities> GatheringAbilities { get; private set; }

        public UnitDataManager()
        {
            UnitData = new Dictionary<UnitTypes, UnitTypeData>();
            UnitAbilities = new Dictionary<Abilities, UnitTypes>();

            ZergTypes = new HashSet<UnitTypes>();
            ProtossTypes = new HashSet<UnitTypes>();
            TerranTypes = new HashSet<UnitTypes>();
            foreach (var name in Enum.GetNames(typeof(UnitTypes)))
            {
                if (name.StartsWith("ZERG"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        ZergTypes.Add(value);
                    }               
                }
                else if (name.StartsWith("PROTOSS"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        ProtossTypes.Add(value);
                    }
                }
                else if (name.StartsWith("TERRAN"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        TerranTypes.Add(value);
                    }
                }
            }

            BuildingData = new Dictionary<UnitTypes, BuildingTypeData>();

            BuildingData.Add(UnitTypes.TERRAN_COMMANDCENTER, new BuildingTypeData { Ability = Abilities.BUILD_COMMANDCENTER, Size = 2, Minerals = 400 });
            BuildingData.Add(UnitTypes.TERRAN_SUPPLYDEPOT, new BuildingTypeData { Ability = Abilities.BUILD_SUPPLYDEPOT, Size = 2, Minerals = 100 });
            BuildingData.Add(UnitTypes.TERRAN_REFINERY, new BuildingTypeData { Ability = Abilities.BUILD_REFINERY, Size = 3, Minerals = 75 });
            BuildingData.Add(UnitTypes.TERRAN_BARRACKS, new BuildingTypeData {Ability = Abilities.BUILD_BARRACKS, Size = 3, Minerals = 150 });
            BuildingData.Add(UnitTypes.TERRAN_ENGINEERINGBAY, new BuildingTypeData { Ability = Abilities.BUILD_ENGINEERINGBAY, Size = 3, Minerals = 125 });
            BuildingData.Add(UnitTypes.TERRAN_MISSILETURRET, new BuildingTypeData { Ability = Abilities.BUILD_MISSILETURRET, Size = 2, Minerals = 100 });
            BuildingData.Add(UnitTypes.TERRAN_BUNKER, new BuildingTypeData { Ability = Abilities.BUILD_BUNKER, Size = 3, Minerals = 100 });
            BuildingData.Add(UnitTypes.TERRAN_SENSORTOWER, new BuildingTypeData { Ability = Abilities.BUILD_SENSORTOWER, Size = 2, Minerals = 125, Gas = 100 });
            BuildingData.Add(UnitTypes.TERRAN_FACTORY, new BuildingTypeData { Ability = Abilities.BUILD_FACTORY, Size = 3, Minerals = 150, Gas = 100 });
            BuildingData.Add(UnitTypes.TERRAN_STARPORT, new BuildingTypeData { Ability = Abilities.BUILD_STARPORT, Size = 2, Minerals = 150, Gas = 100 });
            BuildingData.Add(UnitTypes.TERRAN_ARMORY, new BuildingTypeData { Ability = Abilities.BUILD_ARMORY, Size = 3, Minerals = 150, Gas = 100 });
            BuildingData.Add(UnitTypes.TERRAN_FUSIONCORE, new BuildingTypeData { Ability = Abilities.BUILD_FUSIONCORE, Size = 3, Minerals = 150, Gas = 150 });

            BuildingData.Add(UnitTypes.PROTOSS_NEXUS, new BuildingTypeData { Ability = Abilities.BUILD_NEXUS, Size = 5, Minerals = 400 });
            BuildingData.Add(UnitTypes.PROTOSS_PYLON, new BuildingTypeData { Ability = Abilities.BUILD_PYLON, Size = 2, Minerals = 100 });
            BuildingData.Add(UnitTypes.PROTOSS_ASSIMILATOR, new BuildingTypeData { Ability = Abilities.BUILD_ASSIMILATOR, Size = 3, Minerals = 75 });
            BuildingData.Add(UnitTypes.PROTOSS_GATEWAY, new BuildingTypeData { Ability = Abilities.BUILD_GATEWAY, Size = 3, Minerals = 150 });
            BuildingData.Add(UnitTypes.PROTOSS_FORGE, new BuildingTypeData { Ability = Abilities.BUILD_FORGE, Size = 3, Minerals = 150 });
            BuildingData.Add(UnitTypes.PROTOSS_FLEETBEACON, new BuildingTypeData { Ability = Abilities.BUILD_FLEETBEACON, Size = 3, Minerals = 300, Gas = 200 });
            BuildingData.Add(UnitTypes.PROTOSS_TWILIGHTCOUNCIL, new BuildingTypeData { Ability = Abilities.BUILD_TWILIGHTCOUNCIL, Size = 3, Minerals = 150, Gas = 100 });
            BuildingData.Add(UnitTypes.PROTOSS_PHOTONCANNON, new BuildingTypeData { Ability = Abilities.BUILD_PHOTONCANNON, Size = 2, Minerals = 150 });
            BuildingData.Add(UnitTypes.PROTOSS_STARGATE, new BuildingTypeData { Ability = Abilities.BUILD_STARGATE, Size = 3, Minerals = 150, Gas = 150 });
            BuildingData.Add(UnitTypes.PROTOSS_TEMPLARARCHIVE, new BuildingTypeData { Ability = Abilities.BUILD_TEMPLARARCHIVE, Size = 3, Minerals = 150, Gas = 200 });
            BuildingData.Add(UnitTypes.PROTOSS_DARKSHRINE, new BuildingTypeData { Ability = Abilities.BUILD_DARKSHRINE, Size = 3, Minerals = 150, Gas = 150 });
            BuildingData.Add(UnitTypes.PROTOSS_ROBOTICSBAY, new BuildingTypeData { Ability = Abilities.BUILD_ROBOTICSBAY, Size = 3, Minerals = 200, Gas = 200 });
            BuildingData.Add(UnitTypes.PROTOSS_ROBOTICSFACILITY, new BuildingTypeData { Ability = Abilities.BUILD_ROBOTICSFACILITY, Size = 3, Minerals = 150, Gas = 100 });
            BuildingData.Add(UnitTypes.PROTOSS_CYBERNETICSCORE, new BuildingTypeData { Ability = Abilities.BUILD_CYBERNETICSCORE, Size = 3, Minerals = 150, });
            BuildingData.Add(UnitTypes.PROTOSS_SHIELDBATTERY, new BuildingTypeData { Ability = Abilities.BUILD_SHIELDBATTERY, Size = 2, Minerals = 100 });

            TrainingData = new Dictionary<UnitTypes, TrainingTypeData>();

            TrainingData.Add(UnitTypes.TERRAN_BARRACKSTECHLAB, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Gas = 25, Ability = Abilities.BUILD_TECHLAB_BARRACKS, IsAddOn = true });
            TrainingData.Add(UnitTypes.TERRAN_BARRACKSREACTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Gas = 50, Ability = Abilities.BUILD_REACTOR_BARRACKS, IsAddOn = true });

            TrainingData.Add(UnitTypes.PROTOSS_PROBE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_NEXUS }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_PROBE });
            TrainingData.Add(UnitTypes.PROTOSS_MOTHERSHIP, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_NEXUS }, Minerals = 400, Gas = 400, Food = 8, Ability = Abilities.TRAIN_MOTHERSHIP });
            TrainingData.Add(UnitTypes.PROTOSS_ZEALOT, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 100, Food = 2, Ability = Abilities.TRAIN_ZEALOT, WarpInAbility = Abilities.TRAINWARP_ZEALOT });
            TrainingData.Add(UnitTypes.PROTOSS_SENTRY, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 50, Gas = 100, Food = 2, Ability = Abilities.TRAIN_SENTRY, WarpInAbility = Abilities.TRAINWARP_SENTRY });
            TrainingData.Add(UnitTypes.PROTOSS_STALKER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 125, Gas = 50, Food = 2, Ability = Abilities.TRAIN_STALKER, WarpInAbility = Abilities.TRAINWARP_STALKER });
            TrainingData.Add(UnitTypes.PROTOSS_ADEPT, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 100, Gas = 25, Food = 2, Ability = Abilities.TRAIN_ADEPT, WarpInAbility = Abilities.TRAINWARP_ADEPT });
            TrainingData.Add(UnitTypes.PROTOSS_HIGHTEMPLAR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 50, Gas = 150, Food = 2, Ability = Abilities.TRAIN_HIGHTEMPLAR, WarpInAbility = Abilities.TRAINWARP_HIGHTEMPLAR });
            TrainingData.Add(UnitTypes.PROTOSS_DARKTEMPLAR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 125, Gas = 125, Food = 2, Ability = Abilities.TRAIN_DARKTEMPLAR, WarpInAbility = Abilities.TRAINWARP_DARKTEMPLAR });
            TrainingData.Add(UnitTypes.PROTOSS_OBSERVER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 25, Gas = 75, Food = 1, Ability = Abilities.TRAIN_OBSERVER });
            TrainingData.Add(UnitTypes.PROTOSS_WARPPRISM, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 200, Food = 2, Ability = Abilities.TRAIN_WARPPRISM });
            TrainingData.Add(UnitTypes.PROTOSS_IMMORTAL, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 275, Gas = 100, Food = 4, Ability = Abilities.TRAIN_IMMORTAL });
            TrainingData.Add(UnitTypes.PROTOSS_COLOSSUS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 300, Gas = 200, Food = 6, Ability = Abilities.TRAIN_COLOSSUS });
            TrainingData.Add(UnitTypes.PROTOSS_DISRUPTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 150, Gas = 150, Food = 3, Ability = Abilities.TRAIN_DISRUPTOR });
            TrainingData.Add(UnitTypes.PROTOSS_PHOENIX, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 150, Gas = 100, Food = 2, Ability = Abilities.TRAIN_PHOENIX });
            TrainingData.Add(UnitTypes.PROTOSS_ORACLE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 150, Gas = 150, Food = 3, Ability = Abilities.TRAIN_ORACLE });
            TrainingData.Add(UnitTypes.PROTOSS_VOIDRAY, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 200, Gas = 150, Food = 4, Ability = Abilities.TRAIN_VOIDRAY });
            TrainingData.Add(UnitTypes.PROTOSS_TEMPEST, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 250, Gas = 175, Food = 5, Ability = Abilities.TRAIN_TEMPEST });
            TrainingData.Add(UnitTypes.PROTOSS_CARRIER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 350, Gas = 250, Food = 6, Ability = Abilities.TRAIN_CARRIER });

            UpgradeData = new Dictionary<Upgrades, TrainingTypeData>();
            UpgradeData.Add(Upgrades.WARPGATERESEARCH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 50, Gas = 50, Ability = Abilities.RESEARCH_WARPGATE });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL1 });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL2 });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL3 });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL1 });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL2 });
            UpgradeData.Add(Upgrades.PROTOSSGROUNDARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL3 });
            UpgradeData.Add(Upgrades.PROTOSSAIRWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL1 });
            UpgradeData.Add(Upgrades.PROTOSSAIRWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL2 });
            UpgradeData.Add(Upgrades.PROTOSSAIRWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL3 });
            UpgradeData.Add(Upgrades.PROTOSSAIRARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL1 });
            UpgradeData.Add(Upgrades.PROTOSSAIRARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL2 });
            UpgradeData.Add(Upgrades.PROTOSSAIRARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL3 });
            UpgradeData.Add(Upgrades.PROTOSSSHIELDSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL1 });
            UpgradeData.Add(Upgrades.PROTOSSSHIELDSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL2 });
            UpgradeData.Add(Upgrades.PROTOSSSHIELDSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL3 });
            UpgradeData.Add(Upgrades.GRAVITICDRIVE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GRAVITICDRIVE });
            UpgradeData.Add(Upgrades.EXTENDEDTHERMALLANCE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_EXTENDEDTHERMALLANCE });
            UpgradeData.Add(Upgrades.PSISTORMTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TEMPLARARCHIVE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PSISTORM });
            UpgradeData.Add(Upgrades.DARKTEMPLARBLINKUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_DARKSHRINE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_SHADOWSTRIKE });
            UpgradeData.Add(Upgrades.BLINKTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BLINK });
            UpgradeData.Add(Upgrades.CHARGE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_CHARGE });
            UpgradeData.Add(Upgrades.TECTONICDESTABILIZERS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_TECTONICDESTABILIZERS });
            UpgradeData.Add(Upgrades.PHOENIXRANGEUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PHOENIXANIONPULSECRYSTALS });

            AbilityCooldownTimes = new Dictionary<Abilities, float> { { Abilities.EFFECT_BLINK_STALKER, 10 }, { Abilities.EFFECT_SHADOWSTRIDE, 14 }, { Abilities.EFFECT_TIMEWARP, 7.1f }, { Abilities.EFFECT_PURIFICATIONNOVA, 21.4f }, { Abilities.EFFECT_PSISTORM, 1.43f }, { Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, 42.9f }, { Abilities.EFFECT_ORACLEREVELATION, 10f }, { Abilities.BEHAVIOR_PULSARBEAMON, 4f } };
            WarpInCooldownTimes = new Dictionary<Abilities, float> { { Abilities.TRAINWARP_ADEPT, 20f }, { Abilities.TRAINWARP_DARKTEMPLAR, 32f }, { Abilities.TRAINWARP_HIGHTEMPLAR, 32f }, { Abilities.TRAINWARP_SENTRY, 23f }, { Abilities.TRAINWARP_STALKER, 23f }, { Abilities.TRAINWARP_ZEALOT, 20f } };


            CarryingResourceBuffs = new HashSet<Buffs> {
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGAS,
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGASPROTOSS,
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGASZERG,
                Buffs.CARRYHIGHYIELDMINERALFIELDMINERALS,
                Buffs.CARRYMINERALFIELDMINERALS
            };

            MineralFieldTypes = new HashSet<UnitTypes>
            {
                UnitTypes.NEUTRAL_BATTLESTATIONMINERALFIELD,
                UnitTypes.NEUTRAL_BATTLESTATIONMINERALFIELD750,
                UnitTypes.NEUTRAL_MINERALFIELD,
                UnitTypes.NEUTRAL_MINERALFIELD750,
                UnitTypes.NEUTRAL_PURIFIERMINERALFIELD,
                UnitTypes.NEUTRAL_PURIFIERMINERALFIELD750,
                UnitTypes.NEUTRAL_PURIFIERRICHMINERALFIELD,
                UnitTypes.NEUTRAL_PURIFIERRICHMINERALFIELD750,
                UnitTypes.NEUTRAL_RICHMINERALFIELD,
                UnitTypes.NEUTRAL_RICHMINERALFIELD750,
                UnitTypes.NEUTRAL_LABMINERALFIELD,
                UnitTypes.NEUTRAL_LABMINERALFIELD750
            };

            GasGeyserTypes = new HashSet<UnitTypes>
            {
                UnitTypes.NEUTRAL_VESPENEGEYSER,
                UnitTypes.NEUTRAL_SPACEPLATFORMGEYSER,
                UnitTypes.NEUTRAL_SHAKURASVESPENEGEYSER,
                UnitTypes.NEUTRAL_RICHVESPENEGEYSER,
                UnitTypes.NEUTRAL_PURIFIERVESPENEGEYSER,
                UnitTypes.NEUTRAL_PROTOSSVESPENEGEYSER,
                UnitTypes.ZERG_EXTRACTOR,
                UnitTypes.PROTOSS_ASSIMILATOR,
                UnitTypes.TERRAN_REFINERY
            };

            GasGeyserRefineryTypes = new HashSet<UnitTypes>
            {
                UnitTypes.ZERG_EXTRACTOR,
                UnitTypes.PROTOSS_ASSIMILATOR,
                UnitTypes.TERRAN_REFINERY
            };

            MiningAbilities = new HashSet<Abilities>
            {
                Abilities.HARVEST_GATHER,
                Abilities.HARVEST_GATHER_DRONE,
                Abilities.HARVEST_GATHER_PROBE,
                Abilities.HARVEST_GATHER_SCV,
                Abilities.HARVEST_RETURN,
                Abilities.HARVEST_RETURN_DRONE,
                Abilities.HARVEST_RETURN_MULE,
                Abilities.HARVEST_RETURN_PROBE,
                Abilities.HARVEST_RETURN_SCV
            };
            GatheringAbilities = new HashSet<Abilities>
            {
                Abilities.HARVEST_GATHER,
                Abilities.HARVEST_GATHER_DRONE,
                Abilities.HARVEST_GATHER_PROBE,
                Abilities.HARVEST_GATHER_SCV
            };

            GroundSplashDamagers = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_SIEGETANKSIEGED,
                UnitTypes.TERRAN_PLANETARYFORTRESS,
                UnitTypes.TERRAN_HELLION,
                UnitTypes.TERRAN_HELLIONTANK,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.PROTOSS_ARCHON,
                UnitTypes.PROTOSS_HIGHTEMPLAR,
                UnitTypes.PROTOSS_COLOSSUS,
                UnitTypes.ZERG_BANELING,
                UnitTypes.ZERG_BANELINGBURROWED,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED,
                UnitTypes.ZERG_LURKERMPBURROWED
            };

            AirSplashDamagers = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_THOR,
                UnitTypes.TERRAN_LIBERATOR,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.PROTOSS_ARCHON,
                UnitTypes.PROTOSS_HIGHTEMPLAR,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED,
            };

            CloakableAttackers = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_DARKTEMPLAR,
                UnitTypes.TERRAN_GHOST,
                UnitTypes.TERRAN_BANSHEE
            };

            DetectionTypes = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_MISSILETURRET,
                UnitTypes.PROTOSS_PHOTONCANNON,
                UnitTypes.ZERG_SPORECRAWLER,
                UnitTypes.ZERG_SPORECRAWLERUPROOTED,
                UnitTypes.PROTOSS_OBSERVER,
                UnitTypes.TERRAN_RAVEN,
                UnitTypes.ZERG_OVERSEER
            };

            AbilityDetectionTypes = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ORACLE,
                UnitTypes.TERRAN_GHOST,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED
            };

            NoWeaponCooldownTypes = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ORACLE,
                UnitTypes.PROTOSS_VOIDRAY
            };

            ResourceCenterTypes = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_COMMANDCENTER,
                UnitTypes.TERRAN_COMMANDCENTERFLYING,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.TERRAN_ORBITALCOMMANDFLYING,
                UnitTypes.TERRAN_PLANETARYFORTRESS,
                UnitTypes.PROTOSS_NEXUS,
                UnitTypes.ZERG_HATCHERY,
                UnitTypes.ZERG_LAIR,
                UnitTypes.ZERG_HIVE
            };
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (UnitTypeData unitType in data.Units)
            {
                UnitData.Add((UnitTypes)unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                {
                    UnitAbilities.Add((Abilities)unitType.AbilityId, (UnitTypes)unitType.UnitId);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            ResearchedUpgrades = observation.Observation.RawData.Player.UpgradeIds;
            return new List<SC2APIProtocol.Action>();
        }

        public Weapon GetWeapon(Unit unit)
        {
            var unitType = (UnitTypes)unit.UnitType;
            foreach (Weapon weapon in UnitData[unitType].Weapons)
            {
                if (unitType == UnitTypes.PROTOSS_PHOENIX)
                {
                    if (ResearchedUpgrades.Contains((uint)Upgrades.PHOENIXRANGEUPGRADE))
                    {
                        weapon.Range = 7;
                    }
                }
                if (unitType == UnitTypes.TERRAN_CYCLONE)
                {
                    weapon.Range = 7;
                }
                if (unitType == UnitTypes.PROTOSS_COLOSSUS)
                {
                    if (ResearchedUpgrades.Contains((uint)Upgrades.EXTENDEDTHERMALLANCE))
                    {
                        weapon.Range = 9;
                    }
                }
                return weapon;
            }
            if (unitType == UnitTypes.PROTOSS_SENTRY)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 1 };
            }
            if (unitType == UnitTypes.ZERG_QUEEN)
            {
                return new Weapon { Attacks = 1, Damage = 4, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 0.71f };
            }
            if (unitType == UnitTypes.PROTOSS_VOIDRAY)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 6, Type = Weapon.Types.TargetType.Any, Speed = 0.36f };
            }
            if (unitType == UnitTypes.PROTOSS_ORACLE)
            {
                return new Weapon { Attacks = 1, Damage = 15, Range = 4, Type = Weapon.Types.TargetType.Ground, Speed = 0.61f };
            }
            if (unitType == UnitTypes.ZERG_BANELING || unitType == UnitTypes.ZERG_BANELINGBURROWED)
            {
                return new Weapon { Attacks = 1, Damage = 16, Range = 2.2f, Type = Weapon.Types.TargetType.Ground, Speed = 1f };
            }

            return null;
        }

        public float GetDamage(Unit unit)
        {
            Weapon weaponUsed = GetWeapon(unit);
            if (weaponUsed == null)
                return 0;
            return weaponUsed.Damage;
        }

        public float GetRange(Unit unit)
        {
            Weapon weaponUsed = GetWeapon(unit);
            if (weaponUsed == null)
                return 0;
            return weaponUsed.Range;
        }

        public float GetDps(Unit unit)
        {
            Weapon weaponUsed = GetWeapon(unit);
            if (weaponUsed == null)
                return 0;
            var speed = weaponUsed.Speed;
            if (speed == 0)
                return 0;
            return GetDamage(unit) / weaponUsed.Speed;
        }

        public float GetSightRange(Unit unit)
        {
            return 11; // TODO: get actual sight of units
        }

        public bool CanAttackAir(UnitTypes unitType)
        {
            if (unitType == UnitTypes.PROTOSS_CARRIER || unitType == UnitTypes.TERRAN_WIDOWMINE || unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED 
                || unitType == UnitTypes.TERRAN_CYCLONE || unitType == UnitTypes.ZERG_INFESTOR || unitType == UnitTypes.TERRAN_BATTLECRUISER 
                || unitType == UnitTypes.TERRAN_BUNKER || unitType == UnitTypes.PROTOSS_SENTRY || unitType == UnitTypes.PROTOSS_VOIDRAY)
            {
                return true;
            }

            foreach (Weapon weapon in UnitData[unitType].Weapons)
            {
                if (weapon.Type == Weapon.Types.TargetType.Any || (weapon.Type == Weapon.Types.TargetType.Air))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanAttackGround(UnitTypes unitType)
        {
            if (unitType == UnitTypes.TERRAN_LIBERATORAG || unitType == UnitTypes.PROTOSS_DISRUPTOR || unitType == UnitTypes.PROTOSS_ORACLE || unitType == UnitTypes.PROTOSS_PHOENIX 
                || unitType == UnitTypes.PROTOSS_CARRIER || unitType == UnitTypes.TERRAN_WIDOWMINE || unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED
                || unitType == UnitTypes.TERRAN_CYCLONE || unitType == UnitTypes.ZERG_INFESTOR || unitType == UnitTypes.TERRAN_BATTLECRUISER
                || unitType == UnitTypes.TERRAN_BUNKER || unitType == UnitTypes.PROTOSS_SENTRY || unitType == UnitTypes.PROTOSS_VOIDRAY)
            {
                return true;
            }

            foreach (Weapon weapon in UnitData[unitType].Weapons)
            {
                if (weapon.Type == Weapon.Types.TargetType.Any || (weapon.Type == Weapon.Types.TargetType.Air))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
