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

        /// <summary>
        /// key is ability, value is the unitType it belongs to
        /// </summary>
        Dictionary<Abilities, UnitTypes> UnitAbilities;
        RepeatedField<uint> UpgradeIds;

        public HashSet<UnitTypes> ZergTypes { get; private set; }
        public HashSet<UnitTypes> ProtossTypes { get; private set; }
        public HashSet<UnitTypes> TerranTypes { get; private set; }

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
            UpgradeIds = observation.Observation.RawData.Player.UpgradeIds;
            return new List<SC2APIProtocol.Action>();
        }

        public Weapon GetWeapon(Unit unit)
        {
            var unitType = (UnitTypes)unit.UnitType;
            foreach (Weapon weapon in UnitData[unitType].Weapons)
            {
                if (unitType == UnitTypes.PROTOSS_PHOENIX)
                {
                    if (UpgradeIds.Contains((uint)Upgrades.PHOENIXRANGEUPGRADE))
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
                    if (UpgradeIds.Contains((uint)Upgrades.EXTENDEDTHERMALLANCE))
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
