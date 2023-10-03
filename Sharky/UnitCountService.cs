namespace Sharky
{
    public class UnitCountService
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        FrameToTimeConverter FrameToTimeConverter;

        public UnitCountService(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, FrameToTimeConverter frameToTimeConverter)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            FrameToTimeConverter = frameToTimeConverter;
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

        public bool EnemyHas(UnitTypes unitType)
        {
            return ActiveUnitData.EnemyUnits.Any(u =>  (uint)unitType == u.Value.Unit.UnitType && !u.Value.Unit.IsHallucination);
        }

        public bool EnemyHas(List<UnitTypes> unitTypes)
        {
            return ActiveUnitData.EnemyUnits.Any(u => unitTypes.Any(t => (uint)t == u.Value.Unit.UnitType) && !u.Value.Unit.IsHallucination);
        }

        public bool EnemyHasCompleted(UnitTypes unitType)
        {
            return ActiveUnitData.EnemyUnits.Any(u => (uint)unitType == u.Value.Unit.UnitType && u.Value.Unit.BuildProgress == 1 && !u.Value.Unit.IsHallucination);
        }

        public bool EnemyHasCompleted(List<UnitTypes> unitTypes)
        {
            return ActiveUnitData.EnemyUnits.Any(u => unitTypes.Any(t => (uint)t == u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress == 1 && !u.Value.Unit.IsHallucination);
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
            var inProgress = ActiveUnitData.SelfUnits.Sum(u => GetInProgressCountForUnit(u, unitData));
            if (unitType == UnitTypes.ZERG_ZERGLING)
            {
                return inProgress * 2;
            }
            return inProgress;
        }

        private int GetInProgressCountForUnit(KeyValuePair<ulong, UnitCalculation> u, TrainingTypeData unitData)
        {
            if (u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_EGG
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_LARVA
                || unitData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType)
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_OVERLORDCOCOON
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_BANELINGCOCOON
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_LURKERMPEGG
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_BROODLORDCOCOON
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_RAVAGERCOCOON
                || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_TRANSPORTOVERLORDCOCOON)
            {
                return u.Value.Unit.Orders.Count(o => o.AbilityId == (uint)unitData.Ability);
            }
            return 0;
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
            else if (unitType == UnitTypes.ZERG_SPINECRAWLER)
            {
                count += Count(UnitTypes.ZERG_SPINECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.ZERG_SPORECRAWLER)
            {
                count += Count(UnitTypes.ZERG_SPORECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.ZERG_CREEPTUMOR)
            {
                count += Count(UnitTypes.ZERG_CREEPTUMORBURROWED);
                count += Count(UnitTypes.ZERG_CREEPTUMORQUEEN);
            }
            else if (unitType == UnitTypes.ZERG_EXTRACTOR)
            {
                count += Count(UnitTypes.ZERG_EXTRACTORRICH);
            }

            else if (unitType == UnitTypes.ZERG_DRONE)
            {
                count += Count(UnitTypes.ZERG_DRONEBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_ZERGLING)
            {
                count += Count(UnitTypes.ZERG_ZERGLINGBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_BANELING)
            {
                count += Count(UnitTypes.ZERG_BANELINGBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_ROACH)
            {
                count += Count(UnitTypes.ZERG_ROACHBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_RAVAGER)
            {
                count += Count(UnitTypes.ZERG_RAVAGERBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_HYDRALISK)
            {
                count += Count(UnitTypes.ZERG_HYDRALISKBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_LURKERMP)
            {
                count += Count(UnitTypes.ZERG_LURKERMPBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_INFESTOR)
            {
                count += Count(UnitTypes.ZERG_INFESTORBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_SWARMHOSTMP)
            {
                count += Count(UnitTypes.ZERG_SWARMHOSTBURROWEDMP);
            }
            else if (unitType == UnitTypes.ZERG_QUEEN)
            {
                count += Count(UnitTypes.ZERG_QUEENBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_ULTRALISK)
            {
                count += Count(UnitTypes.ZERG_ULTRALISKBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_OVERLORD)
            {
                count += Count(UnitTypes.ZERG_OVERSEER);
                count += Count(UnitTypes.ZERG_OVERLORDTRANSPORT);
            }

            else if (unitType == UnitTypes.ZERG_CHANGELING)
            {
                count += Count(UnitTypes.ZERG_CHANGELINGMARINE);
                count += Count(UnitTypes.ZERG_CHANGELINGMARINESHIELD);
                count += Count(UnitTypes.ZERG_CHANGELINGZEALOT);
                count += Count(UnitTypes.ZERG_CHANGELINGZERGLING);
                count += Count(UnitTypes.ZERG_CHANGELINGZERGLINGWINGS);
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
            else if (unitType == UnitTypes.TERRAN_TECHLAB)
            {
                count += Count(UnitTypes.TERRAN_BARRACKSTECHLAB);
                count += Count(UnitTypes.TERRAN_FACTORYTECHLAB);
                count += Count(UnitTypes.TERRAN_STARPORTTECHLAB);
            }
            else if (unitType == UnitTypes.TERRAN_REACTOR)
            {
                count += Count(UnitTypes.TERRAN_BARRACKSREACTOR);
                count += Count(UnitTypes.TERRAN_FACTORYREACTOR);
                count += Count(UnitTypes.TERRAN_STARPORTREACTOR);
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
            else if (unitType == UnitTypes.TERRAN_VIKINGASSAULT)
            {
                count += Count(UnitTypes.TERRAN_VIKINGFIGHTER);
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
            else if (unitType == UnitTypes.ZERG_SPINECRAWLER)
            {
                count += Count(UnitTypes.ZERG_SPINECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.ZERG_SPORECRAWLER)
            {
                count += Count(UnitTypes.ZERG_SPORECRAWLERUPROOTED);
            }

            else if (unitType == UnitTypes.ZERG_DRONE)
            {
                count += Count(UnitTypes.ZERG_DRONEBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_ZERGLING)
            {
                count += Count(UnitTypes.ZERG_ZERGLINGBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_BANELING)
            {
                count += Count(UnitTypes.ZERG_BANELINGBURROWED);
                count += Count(UnitTypes.ZERG_BANELINGCOCOON);
            }
            else if (unitType == UnitTypes.ZERG_ROACH)
            {
                count += Count(UnitTypes.ZERG_ROACHBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_RAVAGER)
            {
                count += Count(UnitTypes.ZERG_RAVAGERBURROWED);
                count += Count(UnitTypes.ZERG_RAVAGERCOCOON);
            }
            else if (unitType == UnitTypes.ZERG_HYDRALISK)
            {
                count += Count(UnitTypes.ZERG_HYDRALISKBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_LURKERMP)
            {
                count += Count(UnitTypes.ZERG_LURKERMPBURROWED);
                count += Count(UnitTypes.ZERG_LURKERMPEGG);
            }
            else if (unitType == UnitTypes.ZERG_INFESTOR)
            {
                count += Count(UnitTypes.ZERG_INFESTORBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_SWARMHOSTMP)
            {
                count += Count(UnitTypes.ZERG_SWARMHOSTBURROWEDMP);
            }
            else if (unitType == UnitTypes.ZERG_QUEEN)
            {
                count += Count(UnitTypes.ZERG_QUEENBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_ULTRALISK)
            {
                count += Count(UnitTypes.ZERG_ULTRALISKBURROWED);
            }
            else if (unitType == UnitTypes.ZERG_OVERLORD)
            {
                count += Count(UnitTypes.ZERG_OVERSEER);
                count += Count(UnitTypes.ZERG_OVERLORDTRANSPORT);
            }
            else if (unitType == UnitTypes.ZERG_CHANGELING)
            {
                count += Count(UnitTypes.ZERG_CHANGELINGMARINE);
                count += Count(UnitTypes.ZERG_CHANGELINGMARINESHIELD);
                count += Count(UnitTypes.ZERG_CHANGELINGZEALOT);
                count += Count(UnitTypes.ZERG_CHANGELINGZERGLING);
                count += Count(UnitTypes.ZERG_CHANGELINGZERGLINGWINGS);
            }
            else if (unitType == UnitTypes.ZERG_NYDUSCANAL)
            {
                count += Count(UnitTypes.ZERG_NYDUSNETWORK);
            }
            else if (unitType == UnitTypes.ZERG_NYDUSNETWORK)
            {
                count += Count(UnitTypes.ZERG_NYDUSCANAL);
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
            else if (unitType == UnitTypes.TERRAN_LIBERATOR)
            {
                count += EnemyCount(UnitTypes.TERRAN_LIBERATORAG);
            }
            else if (unitType == UnitTypes.TERRAN_VIKINGASSAULT)
            {
                count += Count(UnitTypes.TERRAN_VIKINGFIGHTER);
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
            else if (unitType == UnitTypes.ZERG_SPINECRAWLER)
            {
                completed += Count(UnitTypes.ZERG_SPINECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.ZERG_SPORECRAWLER)
            {
                completed += Count(UnitTypes.ZERG_SPORECRAWLERUPROOTED);
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

        public bool UpgradeDone(Upgrades upgrade)
        {
            return SharkyUnitData.ResearchedUpgrades.Contains((uint)upgrade);
        }

        public float UpgradeProgress(Upgrades upgrade)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)upgrade)) { return 1; }
            var unitData = SharkyUnitData.UpgradeData[upgrade];
            var upgrader = ActiveUnitData.SelfUnits.Values.FirstOrDefault(u => (unitData.ProducingUnits.Contains((UnitTypes)u.Unit.UnitType)) && u.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            if (upgrader != null)
            {
                return upgrader.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)unitData.Ability).Progress;
            }
            return 0;
        }

        /// <summary>
        /// Gets highest finished status of units of given type. Useful when you want to know when your tech building finishes.
        /// </summary>
        public float TechBuildingProgress(UnitTypes unitType) => ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)unitType).Select(u => u.Unit.BuildProgress).DefaultIfEmpty().Max();

        /// <summary>
        /// Gets ingame elapsed time when the given unit started being produced if it is still in progress.
        /// </summary>
        public TimeSpan BuildingStarted(UnitCalculation unit)
        {
            var buildTime = SharkyUnitData.UnitData[(UnitTypes)unit.Unit.UnitType].BuildTime;
            float startFrame = unit.FrameLastSeen - unit.Unit.BuildProgress * buildTime;
            return FrameToTimeConverter.GetTime(Math.Min(unit.FrameFirstSeen, (int)startFrame));
        }
    }
}
