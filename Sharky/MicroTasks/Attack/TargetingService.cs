using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Attack
{
    public class TargetingService
    {
        ActiveUnitData ActiveUnitData;
        MapDataService MapDataService;
        BaseData BaseData;
        TargetingData TargetingData;

        int EnemyBuildingCount = 0;

        public TargetingService(ActiveUnitData activeUnitData, MapDataService mapDataService, BaseData baseData, TargetingData targetingData)
        {
            ActiveUnitData = activeUnitData;
            MapDataService = mapDataService;
            BaseData = baseData;
            TargetingData = targetingData;
        }

        public Point2D UpdateAttackPoint(Point2D armyPoint, Point2D attackPoint)
        {
            var enemyBuildings = ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure));
            var currentEnemyBuildingCount = enemyBuildings.Count();

            if (MapDataService.SelfVisible(attackPoint) || EnemyBuildingCount != currentEnemyBuildingCount)
            {
                TargetingData.HiddenEnemyBase = false;
                EnemyBuildingCount = currentEnemyBuildingCount;
                var closestEnemyBase = BaseData.BaseLocations.FirstOrDefault(b => enemyBuildings.Any(e => e.Value.Unit.Pos.X == b.Location.X && e.Value.Unit.Pos.Y == b.Location.Y));
                if (closestEnemyBase != null)
                {
                    return closestEnemyBase.Location;
                }

                var enemyBuilding = ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderBy(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(armyPoint.X, armyPoint.Y))).FirstOrDefault().Value;
                if (enemyBuilding != null)
                {
                    return new Point2D { X = enemyBuilding.Unit.Pos.X, Y = enemyBuilding.Unit.Pos.Y };
                }
            }

            if (currentEnemyBuildingCount == 0 && MapDataService.SelfVisible(attackPoint) && MapDataService.Visibility(TargetingData.EnemyMainBasePoint) > 0)
            {
                // can't find enemy base, choose a random base location
                TargetingData.HiddenEnemyBase = true;
                var bases = BaseData.BaseLocations.Where(b => !MapDataService.SelfVisible(b.Location));
                if (bases.Count() == 0)
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
    }
}
