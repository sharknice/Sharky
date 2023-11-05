namespace Sharky.MicroTasks.Attack
{
    public class TargetingService
    {
        ActiveUnitData ActiveUnitData;
        MapDataService MapDataService;
        BaseData BaseData;
        TargetingData TargetingData;

        int EnemyBuildingCount = 0;

        public bool TargetMainFirst { get; set; }
        public bool AvoidEnemyMain { get; set; }

        public TargetingService(ActiveUnitData activeUnitData, MapDataService mapDataService, BaseData baseData, TargetingData targetingData)
        {
            ActiveUnitData = activeUnitData;
            MapDataService = mapDataService;
            BaseData = baseData;
            TargetingData = targetingData;
            TargetMainFirst = false;
        }

        public Point2D UpdateAttackPoint(Point2D armyPoint, Point2D attackPoint)
        {
            if (TargetMainFirst && (BaseData.EnemyBases.Any(e => e.Location.X == TargetingData.EnemyMainBasePoint.X && e.Location.Y == TargetingData.EnemyMainBasePoint.Y) || MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) < 1))
            {
                return TargetingData.EnemyMainBasePoint;
            }

            var enemyBuildings = ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !e.Value.Unit.IsFlying && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMOR && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORQUEEN && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE);
            var currentEnemyBuildingCount = enemyBuildings.Count();

            if (MapDataService.SelfVisible(attackPoint) || EnemyBuildingCount != currentEnemyBuildingCount)
            {
                TargetingData.HiddenEnemyBase = false;
                EnemyBuildingCount = currentEnemyBuildingCount;

                var ordered = ActiveUnitData.EnemyUnits.Where(e => !e.Value.Unit.IsFlying && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMOR && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE && e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderByDescending(e => Vector2.DistanceSquared(e.Value.Position, TargetingData.EnemyArmyCenter));

                UnitCalculation enemyBuilding = null;
                if (AvoidEnemyMain)
                {
                    var height = MapDataService.MapHeight(TargetingData.EnemyMainBasePoint);
                    var vector = TargetingData.EnemyMainBasePoint.ToVector2();
                    enemyBuilding = ordered.Where(e => height != MapDataService.MapHeight(e.Value.Position) || Vector2.DistanceSquared(vector, e.Value.Position) > 225).FirstOrDefault().Value;
                }
                else
                {
                    enemyBuilding = ordered.FirstOrDefault().Value;
                }

                if (enemyBuilding != null)
                {
                    return new Point2D { X = enemyBuilding.Unit.Pos.X, Y = enemyBuilding.Unit.Pos.Y };
                }
                else
                {
                    if (AvoidEnemyMain)
                    {
                        attackPoint = BaseData.EnemyNaturalBase.Location;
                    }
                    else
                    {
                        attackPoint = TargetingData.EnemyMainBasePoint;
                    }
                }
            }

            if (currentEnemyBuildingCount == 0 && MapDataService.SelfVisible(attackPoint) && MapDataService.Visibility(TargetingData.EnemyMainBasePoint) > 0)
            {
                // can't find enemy base, choose a random base location
                TargetingData.HiddenEnemyBase = true;
                var bases = BaseData.BaseLocations.Where(b => !MapDataService.SelfVisible(b.Location));
                if (!bases.Any())
                {
                    // find a random spot on the map and check there
                    return new Point2D { X = new Random().Next(0, MapDataService.MapData.MapWidth), Y = new Random().Next(0, MapDataService.MapData.MapHeight) };
                }
                else
                {
                    return bases.ToList()[new Random().Next(0, bases.Count())].Location;
                }
            }

            return attackPoint;
        }

        public Point2D GetArmyPoint(IEnumerable<UnitCommander> armyUnits, float trimRangeSquared = 100)
        {
            var vectors = armyUnits.Select(u => u.UnitCalculation.Position);
            return GetArmyPoint(vectors, trimRangeSquared);
        }

        public Point2D GetWalkableArmyPoint(IEnumerable<UnitCommander> armyUnits, float trimRangeSquared = 100)
        {
            var vectors = armyUnits.Select(u => u.UnitCalculation.Position);
            var point = GetArmyPoint(vectors, trimRangeSquared);
            if (MapDataService.PathWalkable(point))
            {
                return point;
            }
            var closest = armyUnits.OrderBy(c => Vector2.DistanceSquared(point.ToVector2(), c.UnitCalculation.Position)).FirstOrDefault();
            if (closest != null)
            {
                return closest.UnitCalculation.Position.ToPoint2D();
            }
            return point;
        }

        public Point2D GetArmyPoint(IEnumerable<UnitCalculation> armyUnits, float trimRangeSquared = 100)
        {
            var vectors = armyUnits.Select(u => u.Position);
            return GetArmyPoint(vectors, trimRangeSquared);
        }
        Point2D GetArmyPoint(IEnumerable<Vector2> vectors, float trimRangeSquared)
        {
            if (vectors.Any())
            {
                var average = new Vector2(vectors.Average(v => v.X), vectors.Average(v => v.Y));
                var trimmed = vectors.Where(v => Vector2.DistanceSquared(average, v) < trimRangeSquared);
                if (trimmed.Any())
                {
                    var trimmedAverage = new Point2D { X = trimmed.Average(v => v.X), Y = trimmed.Average(v => v.Y) };
                    return trimmedAverage;
                }
                else
                {
                    return new Point2D { X = average.X, Y = average.Y };
                }
            }
            else
            {
                return TargetingData.ForwardDefensePoint;
            }
        }
    }
}
