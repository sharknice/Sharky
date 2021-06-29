using SC2APIProtocol;

namespace Sharky
{
    public class UnitDataService
    {
        SharkyUnitData SharkyUnitData;

        public UnitDataService(SharkyUnitData sharkyUnitData)
        {
            SharkyUnitData = sharkyUnitData;
        }

        public Weapon GetWeapon(Unit unit)
        {
            var unitType = (UnitTypes)unit.UnitType;
            foreach (Weapon weapon in SharkyUnitData.UnitData[unitType].Weapons)
            {
                if (unitType == UnitTypes.PROTOSS_PHOENIX)
                {
                    if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PHOENIXRANGEUPGRADE))
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
                    if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.EXTENDEDTHERMALLANCE))
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
            if (unitType == UnitTypes.PROTOSS_CARRIER)
            {
                return new Weapon { Attacks = 16, Damage = 5, Range = 14, Type = Weapon.Types.TargetType.Any, Speed = 2.14f };
            }
            if (unitType == UnitTypes.ZERG_BANELING || unitType == UnitTypes.ZERG_BANELINGBURROWED)
            {
                return new Weapon { Attacks = 1, Damage = 16, Range = 2.2f, Type = Weapon.Types.TargetType.Ground, Speed = 1f };
            }
            if (unitType == UnitTypes.TERRAN_BUNKER)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 6, Type = Weapon.Types.TargetType.Any, Speed = 0.15f };
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

            foreach (Weapon weapon in SharkyUnitData.UnitData[unitType].Weapons)
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
            if (unitType == UnitTypes.TERRAN_LIBERATORAG || unitType == UnitTypes.PROTOSS_DISRUPTOR || unitType == UnitTypes.PROTOSS_ORACLE
                || unitType == UnitTypes.PROTOSS_CARRIER || unitType == UnitTypes.TERRAN_WIDOWMINE || unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED
                || unitType == UnitTypes.TERRAN_CYCLONE || unitType == UnitTypes.ZERG_INFESTOR || unitType == UnitTypes.TERRAN_BATTLECRUISER
                || unitType == UnitTypes.TERRAN_BUNKER || unitType == UnitTypes.PROTOSS_SENTRY || unitType == UnitTypes.PROTOSS_VOIDRAY)
            {
                return true;
            }

            foreach (Weapon weapon in SharkyUnitData.UnitData[unitType].Weapons)
            {
                if (weapon.Type == Weapon.Types.TargetType.Any || (weapon.Type == Weapon.Types.TargetType.Ground))
                {
                    return true;
                }
            }
            return false;
        }

        public int CargoSize(UnitTypes unitType)
        {
            if (unitType == UnitTypes.PROTOSS_PROBE)
            {
                return 1;
            }
            if (unitType == UnitTypes.ZERG_DRONE || unitType == UnitTypes.ZERG_ZERGLING)
            {
                return 1;
            }
            if (unitType == UnitTypes.TERRAN_SCV || unitType == UnitTypes.TERRAN_MULE)
            {
                return 1;
            }

            if (unitType == UnitTypes.PROTOSS_ZEALOT || unitType == UnitTypes.PROTOSS_SENTRY || unitType == UnitTypes.PROTOSS_STALKER || unitType == UnitTypes.PROTOSS_ADEPT || unitType == UnitTypes.PROTOSS_HIGHTEMPLAR || unitType == UnitTypes.PROTOSS_DARKTEMPLAR)
            {
                return 2;
            }
            if (unitType == UnitTypes.ZERG_BANELING || unitType == UnitTypes.ZERG_HYDRALISK || unitType == UnitTypes.ZERG_INFESTOR || unitType == UnitTypes.ZERG_QUEEN || unitType == UnitTypes.ZERG_ROACH)
            {
                return 2;
            }
            if (unitType == UnitTypes.TERRAN_MARINE || unitType == UnitTypes.TERRAN_MARAUDER || unitType == UnitTypes.TERRAN_GHOST)
            {
                return 2;
            }

            if (unitType == UnitTypes.PROTOSS_IMMORTAL || unitType == UnitTypes.PROTOSS_DISRUPTOR || unitType == UnitTypes.PROTOSS_ARCHON)
            {
                return 4;
            }
            if (unitType == UnitTypes.ZERG_LURKERMP || unitType == UnitTypes.ZERG_RAVAGER || unitType == UnitTypes.ZERG_SWARMHOSTMP)
            {
                return 4;
            }
            if (unitType == UnitTypes.TERRAN_HELLION || unitType == UnitTypes.TERRAN_HELLIONTANK || unitType == UnitTypes.TERRAN_CYCLONE || unitType == UnitTypes.TERRAN_SIEGETANK)
            {
                return 4;
            }

            if (unitType == UnitTypes.PROTOSS_COLOSSUS)
            {
                return 8;
            }
            if (unitType == UnitTypes.ZERG_ULTRALISK)
            {
                return 8;
            }
            if (unitType == UnitTypes.TERRAN_THOR)
            {
                return 8;
            }

            return 100;
        }
    }
}
