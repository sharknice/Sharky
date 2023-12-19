namespace Sharky
{
    public class UnitDataService
    {
        SharkyUnitData SharkyUnitData;
        SharkyOptions SharkyOptions;
        MacroData MacroData;

        public UnitDataService(SharkyUnitData sharkyUnitData, SharkyOptions sharkyOptions, MacroData macroData)
        {
            SharkyUnitData = sharkyUnitData;
            SharkyOptions = sharkyOptions;
            MacroData = macroData;
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
                else if (unitType == UnitTypes.TERRAN_CYCLONE)
                {
                    weapon.Range = 7;
                }
                else if (unitType == UnitTypes.PROTOSS_COLOSSUS)
                {
                    if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.EXTENDEDTHERMALLANCE))
                    {
                        weapon.Range = 9;
                    }
                }
                else if (unitType == UnitTypes.ZERG_HYDRALISK && MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
                {
                    weapon.Range = 6;
                }
                else if (unitType == UnitTypes.TERRAN_MISSILETURRET && MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
                {
                    weapon.Range = 8;
                }
                else if (unitType == UnitTypes.ZERG_HYDRALISK && MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
                {
                    weapon.Range = 7;
                }
                else if (unitType == UnitTypes.TERRAN_PLANETARYFORTRESS && MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
                {
                    weapon.Range = 7;
                }
                else if (unitType == UnitTypes.TERRAN_AUTOTURRET && MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
                {
                    weapon.Range = 7;
                }
                else if (unitType == UnitTypes.TERRAN_HELLION)
                {
                    if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.HIGHCAPACITYBARRELS))
                    {
                        weapon.DamageBonus[0].Bonus = 11;
                    }
                }
                else if (unitType == UnitTypes.TERRAN_HELLIONTANK)
                {
                    if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.HIGHCAPACITYBARRELS))
                    {
                        if (!weapon.DamageBonus.Any())
                        {
                            weapon.DamageBonus.Add(new DamageBonus { Attribute = SC2Attribute.Light, Bonus = 12 });
                        }
                    }
                }
                return weapon;
            }
            if (unitType == UnitTypes.PROTOSS_SENTRY)
            {
                return new Weapon { Attacks = 1, Damage = 6, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 1 };
            }
            if (unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED)
            {
                return new Weapon { Attacks = 1, Damage = 125, Range = 5, Type = Weapon.Types.TargetType.Any, Speed = 29f };
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
            if (unitType == UnitTypes.TERRAN_BATTLECRUISER)
            {
                return new Weapon { Attacks = 1, Damage = 8, Range = 6, Type = Weapon.Types.TargetType.Any, Speed = 0.16f };
            }
            if (unitType == UnitTypes.PROTOSS_ADEPTPHASESHIFT)
            {
                return SharkyUnitData.UnitData[UnitTypes.PROTOSS_ADEPT].Weapons[0];
            }
            if (unitType == UnitTypes.PROTOSS_DISRUPTOR)
            {
                return new Weapon { Attacks = 1, Damage = 145, Range = 13, Type = Weapon.Types.TargetType.Ground, Speed = 20f };
            }

            return null;
        }

        public float GetDamage(Unit unit)
        {
            Weapon weaponUsed = GetWeapon(unit);
            if (weaponUsed == null)
                return 0;
            return weaponUsed.Damage * weaponUsed.Attacks;
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
                || unitType == UnitTypes.TERRAN_BUNKER || unitType == UnitTypes.PROTOSS_SENTRY || unitType == UnitTypes.PROTOSS_VOIDRAY || unitType == UnitTypes.PROTOSS_ADEPTPHASESHIFT || unitType == UnitTypes.ZERG_BANELING || unitType == UnitTypes.ZERG_BANELINGBURROWED)
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
            if (unitType == UnitTypes.TERRAN_SCV || unitType == UnitTypes.TERRAN_MULE || unitType == UnitTypes.TERRAN_MARINE)
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
            if (unitType == UnitTypes.TERRAN_MARAUDER || unitType == UnitTypes.TERRAN_GHOST || unitType == UnitTypes.TERRAN_VIKINGASSAULT || unitType == UnitTypes.TERRAN_HELLION || unitType == UnitTypes.TERRAN_HELLIONTANK || unitType == UnitTypes.TERRAN_CYCLONE)
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
            if (unitType == UnitTypes.TERRAN_SIEGETANK)
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
