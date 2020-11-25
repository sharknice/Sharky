using Google.Protobuf.Collections;
using SC2APIProtocol;
using Sharky.TypeData;
using System;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class UnitDataManager : SharkyManager
    {
        public Dictionary<UnitTypes, UnitTypeData> UnitData { get; private set; }
        public Dictionary<UnitTypes, BuildingTypeData> BuildingData { get; private set; }
        public Dictionary<UnitTypes, TrainingTypeData> MorphData { get; private set; }
        public Dictionary<UnitTypes, TrainingTypeData> TrainingData { get; private set; }
        public Dictionary<Upgrades, TrainingTypeData> UpgradeData { get; private set; }
        public Dictionary<UnitTypes, TrainingTypeData> AddOnData { get; private set; }

        /// <summary>
        /// key is ability, value is the unitType it belongs to
        /// </summary>
        Dictionary<Abilities, UnitTypes> UnitAbilities;
        public RepeatedField<uint> ResearchedUpgrades;

        public HashSet<UnitTypes> ZergTypes { get; private set; }
        public HashSet<UnitTypes> ProtossTypes { get; private set; }
        public HashSet<UnitTypes> TerranTypes { get; private set; }

        public HashSet<UnitTypes> TechLabTypes { get; private set; }
        public HashSet<UnitTypes> ReactorTypes { get; private set; }

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

        public UnitDataManager(UpgradeDataService upgradeDataService, BuildingDataService buildingDataService, TrainingDataService trainingDataService, AddOnDataService addOnDataService, MorphDataService morphDataService)
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

            BuildingData = buildingDataService.BuildingData();
            TrainingData = trainingDataService.TrainingData();
            UpgradeData = upgradeDataService.UpgradeData();
            AddOnData = addOnDataService.AddOnData();
            MorphData = morphDataService.MorphData();

            AbilityCooldownTimes = new Dictionary<Abilities, float> { { Abilities.EFFECT_BLINK_STALKER, 10 }, { Abilities.EFFECT_SHADOWSTRIDE, 14 }, { Abilities.EFFECT_TIMEWARP, 7.1f }, { Abilities.EFFECT_PURIFICATIONNOVA, 21.4f }, { Abilities.EFFECT_PSISTORM, 1.43f }, { Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, 42.9f }, { Abilities.EFFECT_ORACLEREVELATION, 10f }, { Abilities.BEHAVIOR_PULSARBEAMON, 4f } };
            WarpInCooldownTimes = new Dictionary<Abilities, float> { { Abilities.TRAINWARP_ADEPT, 20f }, { Abilities.TRAINWARP_DARKTEMPLAR, 32f }, { Abilities.TRAINWARP_HIGHTEMPLAR, 32f }, { Abilities.TRAINWARP_SENTRY, 23f }, { Abilities.TRAINWARP_STALKER, 23f }, { Abilities.TRAINWARP_ZEALOT, 20f } };


            TechLabTypes = new HashSet<UnitTypes> {
                UnitTypes.TERRAN_BARRACKSTECHLAB,
                UnitTypes.TERRAN_FACTORYTECHLAB,
                UnitTypes.TERRAN_STARPORTTECHLAB
            };

            ReactorTypes = new HashSet<UnitTypes> {
                UnitTypes.TERRAN_BARRACKSREACTOR,
                UnitTypes.TERRAN_FACTORYREACTOR,
                UnitTypes.TERRAN_STARPORTREACTOR
            };

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
