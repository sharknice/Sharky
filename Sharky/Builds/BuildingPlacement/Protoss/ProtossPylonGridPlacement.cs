using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossPylonGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;   

        public ProtossPylonGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, float maxDistance, float minimumMineralProximinity)
        {
            var targetVector = new Vector2(target.X, target.Y);

            foreach (var selfBase in BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1))
            {
                Point2D closest = null;

                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var otherBaseLocations = BaseData.BaseLocations.Where(b => MapDataService.MapHeight(b.Location) == baseHeight).Select(b => b.Location);
                var mineralLocationVector = new Vector2(selfBase.MineralLineLocation.X, selfBase.MineralLineLocation.Y);
                var xStart = selfBase.Location.X + .5f;
                var yStart = selfBase.Location.Y + 8.5f;

                var x = xStart;
                while (x - xStart < 30)
                {
                    closest = GetClosestValidPoint(target, maxDistance, targetVector, selfBase, closest, baseHeight, otherBaseLocations, mineralLocationVector, yStart, x);
                    x += 10;
                }
                x = xStart - 10;
                while (xStart - x < 30)
                {
                    closest = GetClosestValidPoint(target, maxDistance, targetVector, selfBase, closest, baseHeight, otherBaseLocations, mineralLocationVector, yStart, x);
                    x -= 10;
                }

                if (closest != null)
                {
                    return closest;
                }
            }

            return null;
        }

        private Point2D GetClosestValidPoint(Point2D target, float maxDistance, Vector2 targetVector, BaseLocation selfBase, Point2D closest, int baseHeight, IEnumerable<Point2D> otherBaseLocations, Vector2 mineralLocationVector, float yStart, float x)
        {
            var point = GetValidPointInColumn(x, baseHeight, mineralLocationVector, yStart, maxDistance, target);
            if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
            {
                if (point != null) 
                {
                    var distanceSquared = Vector2.DistanceSquared(new Vector2(point.X, point.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y));
                    if (!otherBaseLocations.Any(b => distanceSquared > Vector2.DistanceSquared(new Vector2(point.X, point.Y), new Vector2(b.X, b.Y))))
                    {
                        closest = point;
                    }
                }
            }

            return closest;
        }

        Point2D GetValidPointInColumn(float x, int baseHeight, Vector2 mineralLocationVector, float yStart, float maxDistance, Point2D target)
        {
            var targetVector = new Vector2(target.X, target.Y);
            Point2D closest = null;

            var y = yStart;
            while (y - yStart < 30)
            {
                closest = GetClosestValidInColumn(x, baseHeight, mineralLocationVector, maxDistance, target, targetVector, closest, y);
                y += 10;
            }
            y = yStart - 10;
            while (yStart - y < 30)
            {
                closest = GetClosestValidInColumn(x, baseHeight, mineralLocationVector, maxDistance, target, targetVector, closest, y);
                y -= 10;
            }

            return closest;
        }

        private Point2D GetClosestValidInColumn(float x, int baseHeight, Vector2 mineralLocationVector, float maxDistance, Point2D target, Vector2 targetVector, Point2D closest, float y)
        {
            var point = GetValidPoint(x, y, baseHeight, mineralLocationVector, maxDistance, target);
            if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
            {
                closest = point;
            }
            var point2 = GetValidPoint(x - 3, y - 1, baseHeight, mineralLocationVector, maxDistance, target);
            if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
            {
                closest = point2;
            }

            return closest;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, Vector2 mineralLocationVector, float maxDistance, Point2D target)
        {
            var size = 2.25f;
            if (Vector2.DistanceSquared(new Vector2(x, y), mineralLocationVector) > 169 && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(target.X, target.Y)) < maxDistance * maxDistance)
            {
                if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight && MapDataService.MapHeight((int)x, (int)y) == baseHeight)
                { 
                    if (BuildingService.AreaBuildable(x, y, size / 2.0f))
                    {
                        if (!BuildingService.BlocksPath(x, y, size / 2.0f))
                        {
                            if (!BuildingService.Blocked(x, y, 1, 0f))
                            {
                                if (!BuildingService.HasAnyCreep(x, y, size / 2.0f) && !BuildingService.BlocksResourceCenter(x, y, 1))
                                {
                                    return new Point2D { X = x, Y = y };
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
