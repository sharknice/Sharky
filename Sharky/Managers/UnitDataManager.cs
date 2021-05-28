using SC2APIProtocol;
using Sharky.TypeData;
using System;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class UnitDataManager : SharkyManager
    {
        SharkyUnitData SharkyUnitData { get; set; }

        public UnitDataManager(UpgradeDataService upgradeDataService, BuildingDataService buildingDataService, TrainingDataService trainingDataService, AddOnDataService addOnDataService, MorphDataService morphDataService, SharkyUnitData sharkyUnitData)
        {
            SharkyUnitData = sharkyUnitData;

            SharkyUnitData.UnitData = new Dictionary<UnitTypes, UnitTypeData>();
            SharkyUnitData.UnitAbilities = new Dictionary<Abilities, UnitTypes>();

            SharkyUnitData.ZergTypes = new HashSet<UnitTypes>();
            SharkyUnitData.ProtossTypes = new HashSet<UnitTypes>();
            SharkyUnitData.TerranTypes = new HashSet<UnitTypes>();
            foreach (var name in Enum.GetNames(typeof(UnitTypes)))
            {
                if (name.StartsWith("ZERG"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        SharkyUnitData.ZergTypes.Add(value);
                    }               
                }
                else if (name.StartsWith("PROTOSS"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        SharkyUnitData.ProtossTypes.Add(value);
                    }
                }
                else if (name.StartsWith("TERRAN"))
                {
                    UnitTypes value;
                    if (Enum.TryParse(name, out value))
                    {
                        SharkyUnitData.TerranTypes.Add(value);
                    }
                }
            }

            SharkyUnitData.BuildingData = buildingDataService.BuildingData();
            SharkyUnitData.TrainingData = trainingDataService.TrainingData();
            SharkyUnitData.UpgradeData = upgradeDataService.UpgradeData();
            SharkyUnitData.AddOnData = addOnDataService.AddOnData();
            SharkyUnitData.MorphData = morphDataService.MorphData();

            SharkyUnitData.AbilityCooldownTimes = new Dictionary<Abilities, float> { { Abilities.EFFECT_BLINK_STALKER, 10 }, { Abilities.EFFECT_SHADOWSTRIDE, 14 }, { Abilities.EFFECT_TIMEWARP, 7.1f }, { Abilities.EFFECT_PURIFICATIONNOVA, 21.4f }, { Abilities.EFFECT_PSISTORM, 1.43f }, { Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, 42.9f }, { Abilities.EFFECT_ORACLEREVELATION, 10f }, { Abilities.BEHAVIOR_PULSARBEAMON, 4f }, { Abilities.EFFECT_KD8CHARGE, 14f }, { Abilities.EFFECT_ADEPTPHASESHIFT, 18f }, { Abilities.NEXUSMASSRECALL, 130f } };
            SharkyUnitData.WarpInCooldownTimes = new Dictionary<Abilities, float> { { Abilities.TRAINWARP_ADEPT, 20f }, { Abilities.TRAINWARP_DARKTEMPLAR, 32f }, { Abilities.TRAINWARP_HIGHTEMPLAR, 32f }, { Abilities.TRAINWARP_SENTRY, 23f }, { Abilities.TRAINWARP_STALKER, 23f }, { Abilities.TRAINWARP_ZEALOT, 20f } };


            SharkyUnitData.TechLabTypes = new HashSet<UnitTypes> {
                UnitTypes.TERRAN_BARRACKSTECHLAB,
                UnitTypes.TERRAN_FACTORYTECHLAB,
                UnitTypes.TERRAN_STARPORTTECHLAB
            };

            SharkyUnitData.ReactorTypes = new HashSet<UnitTypes> {
                UnitTypes.TERRAN_BARRACKSREACTOR,
                UnitTypes.TERRAN_FACTORYREACTOR,
                UnitTypes.TERRAN_STARPORTREACTOR
            };

            SharkyUnitData.CarryingResourceBuffs = new HashSet<Buffs> {
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGAS,
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGASPROTOSS,
                Buffs.CARRYHARVESTABLEVESPENEGEYSERGASZERG,
                Buffs.CARRYHIGHYIELDMINERALFIELDMINERALS,
                Buffs.CARRYMINERALFIELDMINERALS
            };

            SharkyUnitData.CarryingMineralBuffs = new HashSet<Buffs> {
                Buffs.CARRYHIGHYIELDMINERALFIELDMINERALS,
                Buffs.CARRYMINERALFIELDMINERALS
            };

            SharkyUnitData.MineralFieldTypes = new HashSet<UnitTypes>
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

            SharkyUnitData.GasGeyserTypes = new HashSet<UnitTypes>
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

            SharkyUnitData.GasGeyserRefineryTypes = new HashSet<UnitTypes>
            {
                UnitTypes.ZERG_EXTRACTOR,
                UnitTypes.PROTOSS_ASSIMILATOR,
                UnitTypes.TERRAN_REFINERY
            };

            SharkyUnitData.MiningAbilities = new HashSet<Abilities>
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
            SharkyUnitData.GatheringAbilities = new HashSet<Abilities>
            {
                Abilities.HARVEST_GATHER,
                Abilities.HARVEST_GATHER_DRONE,
                Abilities.HARVEST_GATHER_PROBE,
                Abilities.HARVEST_GATHER_SCV
            };

            SharkyUnitData.GroundSplashDamagers = new HashSet<UnitTypes>
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

            SharkyUnitData.AirSplashDamagers = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_THOR,
                UnitTypes.TERRAN_LIBERATOR,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.PROTOSS_ARCHON,
                UnitTypes.PROTOSS_HIGHTEMPLAR,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED,
            };

            SharkyUnitData.CloakableAttackers = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_DARKTEMPLAR,
                UnitTypes.TERRAN_GHOST,
                UnitTypes.TERRAN_BANSHEE,
                UnitTypes.TERRAN_WIDOWMINE,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.ZERG_LURKERMPBURROWED,
                UnitTypes.ZERG_LURKERMP,
                UnitTypes.ZERG_SWARMHOSTBURROWEDMP,
                UnitTypes.ZERG_SWARMHOSTMP,
                UnitTypes.PROTOSS_MOTHERSHIP
            };

            SharkyUnitData.DetectionTypes = new HashSet<UnitTypes>
            {
                UnitTypes.TERRAN_MISSILETURRET,
                UnitTypes.PROTOSS_PHOTONCANNON,
                UnitTypes.ZERG_SPORECRAWLER,
                UnitTypes.ZERG_SPORECRAWLERUPROOTED,
                UnitTypes.PROTOSS_OBSERVER,
                UnitTypes.TERRAN_RAVEN,
                UnitTypes.ZERG_OVERSEER
            };

            SharkyUnitData.AbilityDetectionTypes = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ORACLE,
                UnitTypes.TERRAN_GHOST,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED
            };

            SharkyUnitData.NoWeaponCooldownTypes = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ORACLE,
                UnitTypes.PROTOSS_VOIDRAY
            };

            SharkyUnitData.ResourceCenterTypes = new HashSet<UnitTypes>
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

            SharkyUnitData.DefensiveStructureTypes = new HashSet<UnitTypes>
            {
                UnitTypes.ZERG_SPORECRAWLER,
                UnitTypes.ZERG_SPINECRAWLER,
                UnitTypes.ZERG_SPORECRAWLERUPROOTED,
                UnitTypes.ZERG_SPINECRAWLERUPROOTED,
                UnitTypes.PROTOSS_SHIELDBATTERY,
                UnitTypes.PROTOSS_PHOTONCANNON,
                UnitTypes.TERRAN_PLANETARYFORTRESS,
                UnitTypes.TERRAN_MISSILETURRET,
                UnitTypes.TERRAN_BUNKER
            };
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (UnitTypeData unitType in data.Units)
            {
                SharkyUnitData.UnitData.Add((UnitTypes)unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                {
                    SharkyUnitData.UnitAbilities.Add((Abilities)unitType.AbilityId, (UnitTypes)unitType.UnitId);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            SharkyUnitData.ResearchedUpgrades = observation.Observation.RawData.Player.UpgradeIds;
            SharkyUnitData.Effects = observation.Observation.RawData.Effects;
            return null;
        }
    }
}
