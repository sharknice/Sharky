using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ZergGridPlacement : IBuildingPlacement
    {
        MapDataService MapDataService;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;
        BuildOptions BuildOptions;

        public ZergGridPlacement(DefaultSharkyBot defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BuildingService = defaultSharkyBot.BuildingService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;
        }

        public Point2D FindPlacement(Point2D originalTarget, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = true)
        {
            var target = new Point2D { X = (float)Math.Round(originalTarget.X), Y = (float)Math.Round(originalTarget.Y) };

            var targetVector = new Vector2(target.X, target.Y);
            var baseHeight = MapDataService.MapHeight(target);
            var xStart = target.X;
            var yStart = target.Y;

            Point2D closest = null;
            var x = xStart;
            while (x - xStart < 7)
            {
                var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                }
                x += 1;
            }
            x = xStart - 1;
            while (xStart - x < 7)
            {
                var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase);
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

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, float maxDistance, Vector2 target, bool allowBlockBase)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y += 1;
            }
            y = yStart - 1f;
            while (yStart - y < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y -= 1;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, float maxDistance, Vector2 target, bool allowBlockBase)
        {
            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                !BuildingService.Blocked(x, y, size / 2.0f, 0) && BuildingService.HasCreep(x, y, size / 2f))
            {
                return new Point2D { X = x, Y = y };
            }

            return null;
        }
    }
}
