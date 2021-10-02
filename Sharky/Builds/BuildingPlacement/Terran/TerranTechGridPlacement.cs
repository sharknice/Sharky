using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranTechGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;

        TerranProductionGridPlacement TerranProductionGridPlacement;

        public TerranTechGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService, TerranProductionGridPlacement terranProductionGridPlacement)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;

            TerranProductionGridPlacement = terranProductionGridPlacement;
        }
        public Point2D FindPlacement(Point2D target, UnitTypes unitType, float size, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                // put tech in a grid spot that isn't good for production
                // startX -1, startY +4, X +7/-7, Y +3/-3
                var targetVector = new Vector2(target.X, target.Y);
                var baseVector = new Vector2(selfBase.Location.X, selfBase.Location.Y);
                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var xStart = selfBase.Location.X - 1f;
                var yStart = selfBase.Location.Y + 4f;

                Point2D closest = null;
                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector, baseVector);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x += 7;
                }
                x = xStart - 7;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector, baseVector);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x -= 7;
                }

                if (closest != null)
                {
                    return closest;
                }
                else
                {
                    return TerranProductionGridPlacement.FindPlacement(target, unitType, size, maxDistance, minimumMineralProximinity);
                }
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target, Vector2 baseVector)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target, baseVector);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                y += 3;
            }
            y = yStart -4;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target, baseVector);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                y -= 3;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target, Vector2 baseVector)
        {
            // main building
            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                BuildingService.AreaBuildable(x, y, size / 2.0f) &&
                !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasCreep(x, y, size / 2.0f)
                && BuildingService.RoomBelowAndAbove(x, y, size))
            {
                var addonY = y - .5f;
                var addonX = x + 2.5f;
                var addonVector = new Vector2(addonX, addonY);
                var distanceToBase = Vector2.DistanceSquared(vector, baseVector);
                if (RoomForExitingUnits(x, y, size) || ((vespeneGeysers == null || vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)) || (mineralFields == null || mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16))) && distanceToBase > 16)
                {
                    if (!vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), baseVector) > distanceToBase))
                    {
                        return new Point2D { X = x, Y = y };
                    }
                }
                if (addonX >= 0 && addonY >= 0 && addonX < MapDataService.MapData.MapWidth && addonY < MapDataService.MapData.MapHeight &&
                    MapDataService.MapHeight((int)addonX, (int)addonY) == baseHeight &&
                    BuildingService.AreaBuildable(addonX, addonY, size / 2.0f) &&
                    !BuildingService.Blocked(addonX, addonY, size / 2.0f, -.5f) && !BuildingService.HasCreep(addonX, addonY, size / 2.0f) )
                {
                    return null; 
                }
                else
                {
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }

        bool RoomForExitingUnits(float x, float y, float size)
        {
            return BuildingService.AreaBuildable(x, y, size);
        }
    }
}
