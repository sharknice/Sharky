using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class WarpInPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        DebugService DebugService;
        MapData MapData;
        MapDataService MapDataService;
        BuildingService BuildingService;

        List<Point2D> LastWarpInLocations;

        public WarpInPlacement(ActiveUnitData activeUnitData, DebugService debugService, MapData mapData, MapDataService mapDataService, BuildingService buildingService)
        {
            ActiveUnitData = activeUnitData;
            DebugService = debugService;
            MapData = mapData;
            MapDataService =
            MapDataService = mapDataService;
            BuildingService = buildingService;
            LastWarpInLocations = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreMineralProximity = true, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = true)
        {
            var targetVector = new Vector2(target.X, target.Y);
            Point2D closest = null;

            var powerSources = ActiveUnitData.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING) && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
                var point = FindPlacementForPylon(powerSource.UnitCalculation, size, target);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                }
            }

            if (closest != null)
            {
                LastWarpInLocations.Add(closest);
                if (LastWarpInLocations.Count() > 5)
                {
                    LastWarpInLocations.RemoveAt(0);
                }
            }
            return closest;
        }

        public Point2D FindPlacementForPylon(UnitCalculation powerSource, int size, Point2D target = null)
        {
            if (target == null)
            {
                target = new Point2D { X = powerSource.Unit.Pos.X, Y = powerSource.Unit.Pos.Y };
            }
            var targetVector = new Vector2(target.X, target.Y);

            var baseHeight = MapDataService.MapHeight(powerSource.Unit.Pos);
            var xStart = (float)Math.Round(powerSource.Position.X) + .5f;
            var yStart = (float)Math.Round(powerSource.Position.Y) + 6.5f;

            Point2D closest = null;
            var x = xStart;
            while (x - xStart < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector, powerSource);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                    if (closest != null) { return closest; }
                }
                x += .25f;
            }
            x = xStart - 1;
            while (xStart - x < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector, powerSource);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                {
                    closest = point;
                    if (closest != null) { return closest; }
                }
                x -= .25f;
            }

            return closest;
        }

        Point2D GetValidPointInColumn(float x, int baseHeight, float yStart, Vector2 target, UnitCalculation powerSource)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                var point = GetValidPoint(x, y, baseHeight, target, powerSource);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                    if (closest != null) { return closest; }
                }
                var point2 = GetValidPoint(x + 3, y + 2, baseHeight, target, powerSource);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                    if (closest != null) { return closest; }
                }
                var point3 = GetValidPoint(x + 1, y + 5, baseHeight, target, powerSource);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                    if (closest != null) { return closest; }
                }
                var point4 = GetValidPoint(x - 2, y + 4, baseHeight, target, powerSource);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                    if (closest != null) { return closest; }
                }
                y += .25f;
            }
            y = yStart - 1;
            while (yStart - y < 7)
            {
                var point = GetValidPoint(x, y, baseHeight, target, powerSource);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                    if (closest != null) { return closest; }
                }
                var point2 = GetValidPoint(x + 3, y + 2, baseHeight, target, powerSource);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                    if (closest != null) { return closest; }
                }
                var point3 = GetValidPoint(x + 1, y + 5, baseHeight, target, powerSource);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                    if (closest != null) { return closest; }
                }
                var point4 = GetValidPoint(x - 2, y + 4, baseHeight, target, powerSource);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                    if (closest != null) { return closest; }
                }
                y -= .25f;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, Vector2 target, UnitCalculation powerSource)
        {
            if (LastWarpInLocations.Any(l => l.X == x && l.Y == y))
            { 
                return null; 
            }

            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                BuildingService.AreaBuildable(x, y, .5f) && !BuildingService.Blocked(x, y, .75f, 0f) && !BuildingService.BlockedByUnits(x, y, .75f, powerSource) && Powered(powerSource, x, y))
            {
                return new Point2D { X = x, Y = y };
            }

            return null;
        }

        bool Powered(UnitCalculation powerSource, float x, float y)
        {
            var sourceRadius = 7f;
            if (powerSource.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
            {
                sourceRadius = 5f;
            }

            return Vector2.DistanceSquared(new Vector2(x, y), powerSource.Position) <= sourceRadius;
        }
    }
}
