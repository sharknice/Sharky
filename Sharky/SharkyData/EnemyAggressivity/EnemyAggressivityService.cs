namespace Sharky
{
    public class EnemyAggressivityService
    {
        private EnemyAggressivityData EnemyAggressivityData { get; set; }
        private ActiveUnitData ActiveUnitData { get; set; }
        private EnemyData EnemyData { get; set; }
        private DefaultSharkyBot DefaultSharkyBot { get; set; }

        /// <summary>
        /// Harassing recalc skip
        /// </summary>
        public int FrameSkip { get; set; } = 10;
        private int LastRecalc = 0;

        public EnemyAggressivityService(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            DefaultSharkyBot = defaultSharkyBot;

            EnemyData = defaultSharkyBot.EnemyData;
            EnemyData.EnemyAggressivityData = new EnemyAggressivityData();
            EnemyAggressivityData = EnemyData.EnemyAggressivityData;
            EnemyAggressivityData.DistanceGrid = new DistanceGrid(DefaultSharkyBot);
        }

        public void Update(int frame)
        {
            EnemyAggressivityData.DistanceGrid.Update(frame);

            var grid = EnemyAggressivityData.DistanceGrid;

            float aggressivitySum = 0;
            var unitCount = 0;

            var enemyArmyUnits = ActiveUnitData.EnemyUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));

            EnemyAggressivityData.ArmySupplySize = 0;
            foreach (var unit in enemyArmyUnits)
            {
                var selfDist = grid.GetDist(unit.Unit.Pos.X, unit.Unit.Pos.Y, true, !unit.Unit.IsFlying);
                var enemyDist = grid.GetDist(unit.Unit.Pos.X, unit.Unit.Pos.Y, false, !unit.Unit.IsFlying);

                EnemyAggressivityData.ArmySupplySize += unit.UnitTypeData.FoodRequired;
                aggressivitySum += enemyDist / (selfDist + enemyDist + 1);
                unitCount++;
            }

            if (unitCount == 0)
            {
                EnemyAggressivityData.ArmyAggressivity = 0;
            }
            else
            {
                EnemyAggressivityData.ArmyAggressivity = aggressivitySum / unitCount;
            }

            if (frame - LastRecalc >= FrameSkip)
            {
                UpdateHarassing(enemyArmyUnits, frame);
                LastRecalc = frame;
            }

            // If enemy is attacking and the enemy army can be considered attack and not only harras/scout
            if (EnemyAggressivityData.ArmyAggressivity > 0.75f && EnemyAggressivityData.ArmySupplySize > 5 && EnemyAggressivityData.ArmySupplySize*3 >= DefaultSharkyBot.MacroData.FoodArmy)
            {
                EnemyAggressivityData.LastBigAttackFrame = frame;
            }
        }

        private void UpdateHarassing(IEnumerable<UnitCalculation> enemyUnits, int frame)
        {
            EnemyAggressivityData.HarassingUnits.Clear();
            EnemyAggressivityData.IsAirHarassing = false;
            EnemyAggressivityData.IsGroundHarassing = false;

            // Units that can be considered as harassing with max group size when the group still can be considered harass
            var harassingUnits = new Dictionary<UnitTypes, int>() {
                { UnitTypes.PROTOSS_ORACLE, 5 },
                { UnitTypes.PROTOSS_ADEPT, 8 },
                { UnitTypes.PROTOSS_PHOENIX, 8 },
                { UnitTypes.PROTOSS_ZEALOT, 12 },
                { UnitTypes.PROTOSS_VOIDRAY, 4 },
                { UnitTypes.PROTOSS_DARKTEMPLAR, 8},
                { UnitTypes.PROTOSS_WARPPRISM, 1},
                { UnitTypes.PROTOSS_DISRUPTOR, 1},

                { UnitTypes.TERRAN_BANSHEE, 3},
                { UnitTypes.TERRAN_REAPER, 6},
                { UnitTypes.TERRAN_BATTLECRUISER, 2},
                { UnitTypes.TERRAN_MEDIVAC, 2 },
                { UnitTypes.TERRAN_WIDOWMINE, 4 },
                { UnitTypes.TERRAN_HELLION, 4 },

                { UnitTypes.ZERG_ZERGLING, 16},
                { UnitTypes.ZERG_BANELING, 8},
                { UnitTypes.ZERG_ROACHBURROWED, 10},
                { UnitTypes.ZERG_MUTALISK, 10},
                { UnitTypes.ZERG_CORRUPTOR, 6},
                { UnitTypes.ZERG_LURKERMP, 2},
            };

            foreach (var unit in enemyUnits)
            {
                if (!harassingUnits.TryGetValue((UnitTypes)unit.Unit.UnitType, out var maxCount))
                    continue;

                if (maxCount == 0)
                {
                    continue;
                }

                var dist = EnemyAggressivityData.DistanceGrid.GetDist(unit.Unit.Pos.X, unit.Unit.Pos.Y, true, !unit.Unit.IsFlying);

                if (dist > 15)
                    continue;

                // +1 for some tollerance, for example 
                var nearbySameTypeCount = unit.NearbyAllies.Count(u => u.Unit.UnitType == unit.Unit.UnitType && unit.Unit.Pos.DistanceSquared(u.Unit.Pos) < 100);

                if (nearbySameTypeCount <= maxCount)
                {
                    EnemyAggressivityData.HarassingUnits.Add(unit);

                    if (unit.Unit.IsFlying)
                        EnemyAggressivityData.IsAirHarassing = true;
                    else
                        EnemyAggressivityData.IsGroundHarassing = true;
                }
            }
        }
    }
}
