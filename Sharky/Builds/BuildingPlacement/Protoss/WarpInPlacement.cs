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
                if (maxDistance > 0 && Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 8) * (maxDistance + 8))
                {
                    break;
                }

                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius <= powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 24.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = GetValidPoint(x + (float)(radius * Math.Cos(angle)), y + (float)(radius * Math.Sin(angle)), MapDataService.MapHeight(powerSource.UnitCalculation.Unit.Pos), target.ToVector2(), powerSource.UnitCalculation);
                        if (point != null) 
                        {
                            LastWarpInLocations.Add(point);
                            if (LastWarpInLocations.Count() > 5)
                            {
                                LastWarpInLocations.RemoveAt(0);
                            }
                            return point; 
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
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
            var yStart = (float)Math.Round(powerSource.Position.Y) + .5f;

            Point2D closest = null;
            var x = xStart;
            while (x - xStart < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector, powerSource);
                if (point != null) { UpdateUsedLocations(point); return point; }
                x += .5f;
            }
            x = xStart - 1;
            while (xStart - x < 7)
            {
                var point = GetValidPointInColumn(x, baseHeight, yStart, targetVector, powerSource);
                if (point != null) { UpdateUsedLocations(point); return point; }
                x -= .5f;
            }

            return closest;
        }

        void UpdateUsedLocations(Point2D point)
        {
            LastWarpInLocations.Add(point);
            if (LastWarpInLocations.Count() > 5)
            {
                LastWarpInLocations.RemoveAt(0);
            }
        }

        Point2D GetValidPointInColumn(float x, int baseHeight, float yStart, Vector2 target, UnitCalculation powerSource)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 7)
            {
                for (var xMod = -7f; xMod <= 7; xMod += .25f)
                {
                    var point = GetValidPoint(x + xMod, y, baseHeight, target, powerSource);
                    if (point != null)
                    {
                        return point;
                    }
                }
                y += .5f;
            }
            y = yStart - 1;
            while (yStart - y < 7)
            {
                for (var xMod = -7f; xMod <= 7; xMod += .25f)
                {
                    var point = GetValidPoint(x + xMod, y, baseHeight, target, powerSource);
                    if (point != null)
                    {
                        return point;
                    }
                }
                y -= .5f;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, Vector2 target, UnitCalculation powerSource)
        {
            if (LastWarpInLocations.Any(l => l.X == x && l.Y == y))
            { 
                return null; 
            }

            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight)
            {
                if (MapDataService.PathWalkable(new Point2D { X = x, Y = y, }, 1) && !BuildingService.Blocked(x, y, .75f, 0f) && !BuildingService.BlockedByUnits(x, y, .75f, powerSource) && Powered(powerSource, x, y))
                {
                    return new Point2D { X = x, Y = y };
                }
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

            return Vector2.DistanceSquared(new Vector2(x, y), powerSource.Position) <= sourceRadius * sourceRadius;
        }
    }
}
