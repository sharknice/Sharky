using Google.Protobuf.Collections;
using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class UnitDataManager : SharkyManager
    {
        public Dictionary<uint, UnitTypeData> UnitData { get; private set; }
        /// <summary>
        /// key is ability, value is the unitType it belongs to
        /// </summary>
        Dictionary<uint, uint> UnitAbilities;
        RepeatedField<uint> UpgradeIds;

        public HashSet<UnitTypes> ZergTypes { get; private set; }
        public HashSet<UnitTypes> ProtossTypes { get; private set; }
        public HashSet<UnitTypes> TerranTypes { get; private set; }

        public UnitDataManager()
        {
            UnitData = new Dictionary<uint, UnitTypeData>();
            UnitAbilities = new Dictionary<uint, uint>();

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
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (UnitTypeData unitType in data.Units)
            {
                UnitData.Add(unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                {
                    UnitAbilities.Add(unitType.AbilityId, unitType.UnitId);
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
            foreach (Weapon weapon in UnitData[unit.UnitType].Weapons)
            {
                if (unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX)
                {
                    if (UpgradeIds.Contains((uint)Upgrades.PHOENIXRANGEUPGRADE))
                    {
                        weapon.Range = 7;
                    }
                }
                if (unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE)
                {
                    weapon.Range = 7;
                }
                if (unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS)
                {
                    if (UpgradeIds.Contains((uint)Upgrades.EXTENDEDTHERMALLANCE))
                    {
                        weapon.Range = 9;
                    }
                }
                return weapon;
            }
            if (unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 1 };
            }
            if (unit.UnitType == (uint)UnitTypes.ZERG_QUEEN)
            {
                return new Weapon { Attacks = 1, Damage = 4, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 0.71f };
            }
            if (unit.UnitType == (uint)UnitTypes.PROTOSS_VOIDRAY)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 6, Type = Weapon.Types.TargetType.Any, Speed = 0.36f };
            }
            if (unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE)
            {
                return new Weapon { Attacks = 1, Damage = 15, Range = 4, Type = Weapon.Types.TargetType.Ground, Speed = 0.61f };
            }
            if (unit.UnitType == (uint)UnitTypes.ZERG_BANELING || unit.UnitType == (uint)UnitTypes.ZERG_BANELINGBURROWED)
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

        public bool CanAttackAir(uint unitType)
        {
            if (unitType == (uint)UnitTypes.PROTOSS_CARRIER || unitType == (uint)UnitTypes.TERRAN_WIDOWMINE || unitType == (uint)UnitTypes.TERRAN_WIDOWMINEBURROWED 
                || unitType == (uint)UnitTypes.TERRAN_CYCLONE || unitType == (uint)UnitTypes.ZERG_INFESTOR || unitType == (uint)UnitTypes.TERRAN_BATTLECRUISER 
                || unitType == (uint)UnitTypes.TERRAN_BUNKER || unitType == (uint)UnitTypes.PROTOSS_SENTRY || unitType == (uint)UnitTypes.PROTOSS_VOIDRAY)
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

        public bool CanAttackGround(uint unitType)
        {
            if (unitType == (uint)UnitTypes.TERRAN_LIBERATORAG || unitType == (uint)UnitTypes.PROTOSS_DISRUPTOR || unitType == (uint)UnitTypes.PROTOSS_ORACLE || unitType == (uint)UnitTypes.PROTOSS_PHOENIX 
                || unitType == (uint)UnitTypes.PROTOSS_CARRIER || unitType == (uint)UnitTypes.TERRAN_WIDOWMINE || unitType == (uint)UnitTypes.TERRAN_WIDOWMINEBURROWED
                || unitType == (uint)UnitTypes.TERRAN_CYCLONE || unitType == (uint)UnitTypes.ZERG_INFESTOR || unitType == (uint)UnitTypes.TERRAN_BATTLECRUISER
                || unitType == (uint)UnitTypes.TERRAN_BUNKER || unitType == (uint)UnitTypes.PROTOSS_SENTRY || unitType == (uint)UnitTypes.PROTOSS_VOIDRAY)
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
