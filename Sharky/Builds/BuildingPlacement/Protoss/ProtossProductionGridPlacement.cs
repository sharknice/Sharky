using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossProductionGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;

        List<Point2D> LastLocations;

        public ProtossProductionGridPlacement(BaseData baseData, ActiveUnitData activeUnitData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;
            ActiveUnitData = activeUnitData;

            LastLocations = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                //g1 0 + 6, 57.5 66.5
                //g2 + 3 + 8, 60.5 68.5
                //g3 + 1 + 11, 58.5 71.5
                //g4 - 2 + 10, 55.5 70.5

                var targetVector = new Vector2(target.X, target.Y);
                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var xStart = selfBase.Location.X;
                var yStart = selfBase.Location.Y + 6f;

                Point2D closest = null;
                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x += 10;
                }
                x = xStart - 10;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x -= 10;
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

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y += 10;
            }
            y = yStart -10;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                var point2 = GetValidPoint(x + 3, y + 2, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point2 != null && Vector2.DistanceSquared(new Vector2(point2.X, point2.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point2;
                }
                var point3 = GetValidPoint(x + 1, y + 5, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point3 != null && Vector2.DistanceSquared(new Vector2(point3.X, point3.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point3;
                }
                var point4 = GetValidPoint(x - 2, y + 4, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target);
                if (closest == null || point4 != null && Vector2.DistanceSquared(new Vector2(point4.X, point4.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point4;
                }
                y -= 10;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target)
        {
            if (LastLocations.Any(l => l.X == x && l.Y == y))
            {
                return null;
            }

            if (BuildingService.BlocksResourceCenter(x, y, size / 2f))
            {
                return null;
            }

            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                RoomForExitingUnits(x, y, size) &&
                !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasAnyCreep(x, y, size / 2f) &&
                (mineralFields == null || !mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16)) &&
                (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)) &&
                BuildingService.RoomBelowAndAbove(x, y, size) && !BlocksWall(vector))
            {
                if (ActiveUnitData.Commanders.Values.Any(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1 && Vector2.DistanceSquared(c.UnitCalculation.Position, vector) < 42.25))
                {
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }

        bool RoomForExitingUnits(float x, float y, float size)
        {
            return BuildingService.AreaBuildable(x, y, size / 2.0f);
        }

        bool BlocksWall(Vector2 vector)
        {
            foreach (var wallData in MapDataService.MapData.WallData.Where(b => BaseData.BaseLocations.Take(2).Any(l => l.Location.X == b.BasePosition.X && l.Location.Y == b.BasePosition.Y)))
            {
                if (wallData.Block != null)
                {
                    var blockVector = new Vector2(wallData.Block.X, wallData.Block.Y);
                    if (Vector2.DistanceSquared(blockVector, vector) < 50)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
