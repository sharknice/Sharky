namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossDefensiveGridPlacement : IBuildingPlacement
    {
        MapDataService MapDataService;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;
        BuildOptions BuildOptions;
        SharkyUnitData SharkyUnitData;

        List<Point2D> LastLocations;

        public ProtossDefensiveGridPlacement(DefaultSharkyBot defaultSharkyBot)
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
            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 14) * (maxDistance + 14))
                {
                    break;
                }

                var baseHeight = MapDataService.MapHeight(powerSource.UnitCalculation.Unit.Pos);
                if (requireSameHeight && baseHeight != MapDataService.MapHeight(target))
                {
                    continue;
                }
                var xStart = powerSource.UnitCalculation.Unit.Pos.X;
                var yStart = powerSource.UnitCalculation.Unit.Pos.Y + 6f;

                Point2D closest = null;
                var x = xStart;
                while (x - xStart < 7)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase, ignoreResourceProximity);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x += 1;
                }
                x = xStart - 1;
                while (xStart - x < 7)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, maxDistance, targetVector, allowBlockBase, ignoreResourceProximity);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x -= 1;
                }

                if (closest != null)
                {
                    LastLocations.Add(closest);
                    if (LastLocations.Count() > 5)
                    {
                        LastLocations.RemoveAt(0);
                    }

                    return closest;
                }
            }
            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, float maxDistance, Vector2 target, bool allowBlockBase, bool ignoreResourceProximity)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y += 1;
            }
            y = yStart - 1;
            while (yStart - y < 7)
            {
                var point = GetValidPoint(x, y, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, maxDistance, target, allowBlockBase, ignoreResourceProximity);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y -= 1;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, float maxDistance, Vector2 target, bool allowBlockBase, bool ignoreResourceProximity)
        {
            if (LastLocations.Any(l => l.X == x && l.Y == y))
            {
                return null;
            }

            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                !BuildingService.Blocked(x, y, size / 2.0f, 0) && !BuildingService.HasAnyCreep(x, y, size / 2f) &&
                BuildingService.RoomBelowAndAbove(x, y, size))
            {
                if (!BuildOptions.AllowBlockWall && MapDataService.MapData?.WallData != null && MapDataService.MapData.WallData.Any(d => d.FullDepotWall != null && d.FullDepotWall.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 16)))
                {
                    return null;
                }

                if (!BuildOptions.AllowBlockWall && MapDataService.MapData?.WallData != null && MapDataService.MapData.WallData.Any(d => d.Block != null && Vector2.DistanceSquared(new Vector2(d.Block.X, d.Block.Y), vector) < 16))
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

                if (mineralProximity > 0)
                {
                    var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                    var gasFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                    var squared = (.5f + mineralProximity + (size / 2f)) * (.5f + mineralProximity + (size / 2f));
                    var gasSquared = (2.5f + mineralProximity + (size / 2f)) * (2.5f + mineralProximity + (size / 2f));
                    var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, vector) < squared);
                    var gasClashes = gasFields.Where(u => Vector2.DistanceSquared(u.Value.Position, vector) < gasSquared);
                    if (clashes.Any() || gasClashes.Any())
                    {
                        return null;
                    }
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
