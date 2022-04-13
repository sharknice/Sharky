using System.Linq;

namespace Sharky
{
    public class UnitCountService
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;

        public UnitCountService(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
        }

        public int Count(UnitTypes unitType)
        {
            return ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType && !u.Value.Unit.IsHallucination);
        }

        public int EnemyCount(UnitTypes unitType)
        {
            return ActiveUnitData.EnemyUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType && !u.Value.Unit.IsHallucination);
        }

        public int EnemyCompleted(UnitTypes unitType)
        {
            return ActiveUnitData.EnemyUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType && u.Value.Unit.BuildProgress == 1 && !u.Value.Unit.IsHallucination);
        }

        public int UnitsDoneAndInProgressCount(UnitTypes unitType)
        {
            return EquivalentTypeCount(unitType) + UnitsInProgressCount(unitType);
        }

        public int BuildingsDoneAndInProgressCount(UnitTypes unitType)
        {
            return EquivalentTypeCount(unitType) + BuildingsInProgressCount(unitType);
        }

        public int BuildingsInProgressCount(UnitTypes unitType)
        {
            if (SharkyUnitData.BuildingData.ContainsKey(unitType))
            {
                var unitData = SharkyUnitData.BuildingData[unitType];
                var inProgress = ActiveUnitData.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.Worker) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                return inProgress;
            }
            else if (SharkyUnitData.MorphData.ContainsKey(unitType))
            {
                var unitData = SharkyUnitData.MorphData[unitType];
                var inProgress = ActiveUnitData.SelfUnits.Count(u => unitData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                return inProgress;
            }
            return 0;
        }

        public int UnitsInProgressCount(UnitTypes unitType)
        {
            var unitData = SharkyUnitData.TrainingData[unitType];
            var inProgress = ActiveUnitData.SelfUnits.Count(u => (unitData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_EGG || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_OVERLORDCOCOON || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_TRANSPORTOVERLORDCOCOON) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            if (unitType == UnitTypes.ZERG_ZERGLING)
            {
                return inProgress * 2;
            }
            return inProgress;
        }

        public int EquivalentTypeCount(UnitTypes unitType)
        {
            var count = Count(unitType);
            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                count += Count(UnitTypes.PROTOSS_WARPGATE);
            }

            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                count += Count(UnitTypes.ZERG_HIVE);
                count += Count(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.ZERG_LAIR)
            {
                count += Count(UnitTypes.ZERG_HIVE);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                count += Count(UnitTypes.ZERG_GREATERSPIRE);
            }

            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                count += Count(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                count += Count(UnitTypes.TERRAN_ORBITALCOMMAND);
                count += Count(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                count += Count(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_ORBITALCOMMAND)
            {
                count += Count(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                count += Count(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                count += Count(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                count += Count(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                count += Count(UnitTypes.TERRAN_STARPORTFLYING);
            }

            else if (unitType == UnitTypes.TERRAN_HELLION)
            {
                count += Count(UnitTypes.TERRAN_HELLIONTANK);
            }
            else if (unitType == UnitTypes.TERRAN_HELLIONTANK)
            {
                count += Count(UnitTypes.TERRAN_HELLION);
            }
            else if (unitType == UnitTypes.TERRAN_WIDOWMINE)
            {
                count += Count(UnitTypes.TERRAN_WIDOWMINEBURROWED);
            }
            else if (unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED)
            {
                count += Count(UnitTypes.TERRAN_WIDOWMINE);
            }
            else if (unitType == UnitTypes.TERRAN_SIEGETANK)
            {
                count += Count(UnitTypes.TERRAN_SIEGETANKSIEGED);
            }
            else if (unitType == UnitTypes.TERRAN_SIEGETANKSIEGED)
            {
                count += Count(UnitTypes.TERRAN_SIEGETANK);
            }
            else if (unitType == UnitTypes.TERRAN_THOR)
            {
                count += Count(UnitTypes.TERRAN_THORAP);
            }
            else if (unitType == UnitTypes.TERRAN_THORAP)
            {
                count += Count(UnitTypes.TERRAN_THOR);
            }

            else if (unitType == UnitTypes.PROTOSS_WARPPRISM)
            {
                count += Count(UnitTypes.PROTOSS_WARPPRISMPHASING);
            }

            return count;
        }

        public int EquivalentEnemyTypeCount(UnitTypes unitType)
        {
            var count = EnemyCount(unitType);
            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                count += EnemyCount(UnitTypes.PROTOSS_WARPGATE);
            }
            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                count += EnemyCount(UnitTypes.ZERG_HIVE);
                count += EnemyCount(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.ZERG_LAIR)
            {
                count += EnemyCount(UnitTypes.ZERG_HIVE);
            }
            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                count += EnemyCount(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                count += EnemyCount(UnitTypes.TERRAN_ORBITALCOMMAND);
                count += EnemyCount(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                count += EnemyCount(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_ORBITALCOMMAND)
            {
                count += EnemyCount(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                count += EnemyCount(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                count += EnemyCount(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                count += EnemyCount(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                count += EnemyCount(UnitTypes.TERRAN_STARPORTFLYING);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                count += EnemyCount(UnitTypes.ZERG_GREATERSPIRE);
            }

            else if (unitType == UnitTypes.TERRAN_HELLION)
            {
                count += EnemyCount(UnitTypes.TERRAN_HELLIONTANK);
            }
            else if (unitType == UnitTypes.TERRAN_HELLIONTANK)
            {
                count += EnemyCount(UnitTypes.TERRAN_HELLION);
            }
            else if (unitType == UnitTypes.TERRAN_WIDOWMINE)
            {
                count += EnemyCount(UnitTypes.TERRAN_WIDOWMINEBURROWED);
            }
            else if (unitType == UnitTypes.TERRAN_WIDOWMINEBURROWED)
            {
                count += EnemyCount(UnitTypes.TERRAN_WIDOWMINE);
            }
            else if (unitType == UnitTypes.TERRAN_SIEGETANK)
            {
                count += EnemyCount(UnitTypes.TERRAN_SIEGETANKSIEGED);
            }
            else if (unitType == UnitTypes.TERRAN_SIEGETANKSIEGED)
            {
                count += EnemyCount(UnitTypes.TERRAN_SIEGETANK);
            }
            else if (unitType == UnitTypes.TERRAN_THOR)
            {
                count += EnemyCount(UnitTypes.TERRAN_THORAP);
            }
            else if (unitType == UnitTypes.TERRAN_THORAP)
            {
                count += EnemyCount(UnitTypes.TERRAN_THOR);
            }

            else if (unitType == UnitTypes.PROTOSS_WARPPRISM)
            {
                count += EnemyCount(UnitTypes.PROTOSS_WARPPRISMPHASING);
            }

            return count;
        }

        public int Completed(UnitTypes unitType)
        {
            return ActiveUnitData.SelfUnits.Count(u => !u.Value.Unit.IsHallucination && u.Value.Unit.UnitType == (uint)unitType && 
                (u.Value.Unit.BuildProgress == 1 ||
                (u.Value.Unit.BuildProgress > .99f && (u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS))));
        }

        public int EquivalentTypeCompleted(UnitTypes unitType)
        {
            var completed = Completed(unitType);
            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                completed += Completed(UnitTypes.PROTOSS_WARPGATE);
            }
            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                completed += Completed(UnitTypes.ZERG_HIVE);
                completed += Completed(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.ZERG_LAIR)
            {
                completed += Completed(UnitTypes.ZERG_HIVE);
            }
            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                completed += Completed(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                completed += Completed(UnitTypes.TERRAN_ORBITALCOMMAND);
                completed += Completed(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                completed += Completed(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_ORBITALCOMMAND)
            {
                completed += Completed(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                completed += Completed(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                completed += Completed(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                completed += Completed(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                completed += Completed(UnitTypes.TERRAN_STARPORTFLYING);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                completed += Completed(UnitTypes.ZERG_GREATERSPIRE);
            }

            return completed;
        }

        public bool UpgradeDoneOrInProgress(Upgrades upgrade)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)upgrade)) { return true; }
            var unitData = SharkyUnitData.UpgradeData[upgrade];
            var inProgress = ActiveUnitData.SelfUnits.Any(u => (unitData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType)) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            return inProgress;
        }
    }
}
