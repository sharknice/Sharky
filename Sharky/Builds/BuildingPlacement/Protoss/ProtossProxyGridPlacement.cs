﻿namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossProxyGridPlacement : IBuildingPlacement
    {
        MapDataService MapDataService;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;
        BuildOptions BuildOptions;
        SharkyUnitData SharkyUnitData;

        List<Point2D> LastLocations;

        public ProtossProxyGridPlacement(DefaultSharkyBot defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BuildingService = defaultSharkyBot.BuildingService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            LastLocations = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = false)
        {
            var targetVector = new Vector2(target.X, target.Y);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));

            var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(unit => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)unit.Unit.UnitType)).Select(u => u.Unit);
            var vespeneGeysers = ActiveUnitData.NeutralUnits.Values.Where(unit => SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)unit.Unit.UnitType)).Select(u => u.Unit);

            var startingPoint = GetValidPoint(target.X, target.Y, size, MapDataService.MapHeight(target), maxDistance, new Vector2(target.X, target.Y), allowBlockBase, mineralFields, vespeneGeysers);
            if (startingPoint != null)
            {
                return startingPoint;
            }

            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 14) * (maxDistance + 14))
                {
                    break;
                }

                var baseHeight = MapDataService.MapHeight(powerSource.UnitCalculation.Unit.Pos);
                var xStart = powerSource.UnitCalculation.Unit.Pos.X - .5f;
                var yStart = powerSource.UnitCalculation.Unit.Pos.Y + 7.5f;

                Point2D closest = null;
                var x = xStart;
                while (x - xStart < 7)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase, mineralFields, vespeneGeysers);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x += 1;
                }
                x = xStart - 1;
                while (xStart - x < 7)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase, mineralFields, vespeneGeysers);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x -= 1;
                }

                if (closest != null)
                {
                    LastLocations.Add(closest);
                    if (LastLocations.Count() > 10)
                    {
                        LastLocations.RemoveAt(0);
                    }
                    return closest;
                }
            }
            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, float maxDistance, Vector2 target, bool allowBlockBase, IEnumerable<Unit> mineralFields, IEnumerable<Unit> vespeneGeysers)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y += 1;
            }
            y = yStart - 1f;
            while (yStart - y < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase, mineralFields, vespeneGeysers);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y -= 1;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, float maxDistance, Vector2 target, bool allowBlockBase, IEnumerable<Unit> mineralFields, IEnumerable<Unit> vespeneGeysers)
        {
            if (LastLocations.Any(l => l.X == x && l.Y == y))
            {
                return null;
            }

            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                !BuildingService.Blocked(x, y, size / 2.0f, 0) && !BuildingService.HasAnyCreep(x, y, size / 2f) &&
                BuildingService.SameHeight(x, y, .25f + (size / 2.0f)) &&
                (mineralFields == null || !mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16)) &&
                (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)) &&
                (allowBlockBase || BuildingService.RoomBelowAndAbove(x, y, size)))
            {
                if (!BuildOptions.AllowBlockWall && MapDataService.MapData?.WallData != null && MapDataService.MapData.WallData.Any(d => d.FullDepotWall != null && d.FullDepotWall.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 16)))
                {
                    return null;
                }

                if (!allowBlockBase && BuildingService.BlocksResourceCenter(x, y, size / 2f))
                {
                    return null;
                }

                if (BuildingService.InRangeOfEnemy(x, y, size))
                {
                    return null;
                }

                if (ActiveUnitData.Commanders.Values.Any(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1 && Vector2.DistanceSquared(c.UnitCalculation.Position, vector) < 42.25))
                {
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }
    }
}
