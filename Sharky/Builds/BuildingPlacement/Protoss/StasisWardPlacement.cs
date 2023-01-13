using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class StasisWardPlacement
    {
        BuildingService BuildingService;
        MapDataService MapDataService;

        public StasisWardPlacement(DefaultSharkyBot defaultSharkyBot)
        {
            BuildingService = defaultSharkyBot.BuildingService;
            MapDataService = defaultSharkyBot.MapDataService;
        }

        // 1x1, on .5
        public Point2D FindPlacement(Point2D target)
        {
            var targetVector = new Vector2(target.X, target.Y);

            var baseHeight = MapDataService.MapHeight(target);
            var xStart = (float)Math.Round(target.X) + .5f;
            var yStart = (float)Math.Round(target.Y) + 6.5f;

            Point2D closest = null;
            var x = xStart;
            while (x - xStart < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                }
                x += 1;
            }
            x = xStart - 1;
            while (xStart - x < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                }
                x -= 1;
            }

            if (closest != null)
            {
                return closest;
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, int baseHeight, float yStart, Vector2 target)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                var point = GetValidPoint(x, y, baseHeight, target);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, baseHeight, target);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, baseHeight, target);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, baseHeight, target);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y += 1;
            }
            y = yStart - 1;
            while (yStart - y < 7)
            {
                var point = GetValidPoint(x, y, baseHeight, target);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, baseHeight, target);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, baseHeight, target);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, baseHeight, target);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y -= 1;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, Vector2 target)
        {          
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                BuildingService.AreaBuildable(x, y, .5f) && !BuildingService.BlockedByStructuresOrMinerals(x, y, .5f, 0f) && !BuildingService.BlockedByEnemyUnits(x, y, .5f))
            {
                return new Point2D { X = x, Y = y };
            }
            
            return null;
        }
    }
}
