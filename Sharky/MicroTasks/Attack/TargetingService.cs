using SC2APIProtocol;
using Sharky.Managers;
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

        public TargetingService(ActiveUnitData activeUnitData, MapDataService mapDataService, BaseData baseData)
        {
            ActiveUnitData = activeUnitData;
            MapDataService = mapDataService;
            BaseData = baseData;
        }

        public Point2D UpdateAttackPoint(Point2D armyPoint, Point2D attackPoint)
        {
            var enemyBuilding = ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderBy(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(armyPoint.X, armyPoint.Y))).FirstOrDefault().Value;
            if (enemyBuilding != null)
            {
                return new Point2D { X = enemyBuilding.Unit.Pos.X, Y = enemyBuilding.Unit.Pos.Y };
            }

            // if we have vision of AttackPoint find a new AttackPoint, choose a random base location
            if (MapDataService.SelfVisible(attackPoint))
            {
                var bases = BaseData.BaseLocations.Where(b => !MapDataService.SelfVisible(b.Location));
                if (bases.Count() == 0)
                {
                    // find a random spot on the map and check there
                    return new Point2D { X = new Random().Next(0, MapDataService.MapData.MapWidth), Y = new Random().Next(0, MapDataService.MapData.MapHeight) };
                }
                else
                {
                    return bases.ToList()[new Random().Next(0, bases.Count() - 1)].Location;
                }
            }
            return attackPoint;
        }
    }
}
