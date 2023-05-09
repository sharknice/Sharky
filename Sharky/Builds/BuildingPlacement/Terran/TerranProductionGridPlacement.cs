using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranProductionGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;   

        public TerranProductionGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, float size, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                // grid placement for production and tech, put tech in same spot a production building would go
                // startX -1, startY +4, X +7/-7, Y +3/-3
                var targetVector = new Vector2(target.X, target.Y);
                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var xStart = selfBase.Location.X - 1f;
                var yStart = selfBase.Location.Y + 4f;

                Point2D closest = null;
                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector, unitType);
                    if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), targetVector) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), targetVector))
                    {
                        closest = point;
                    }
                    x += 7;
                }
                x = xStart - 7;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector, unitType);
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
                else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
                {
                    return null;
                }
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, float yStart, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target, UnitTypes unitType)
        {
            Point2D closest = null;
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target, unitType);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                y += 3;
            }
            y = yStart -4;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralFields, vespeneGeysers, maxDistance, target, unitType);
                if (closest == null || point != null && Vector2.DistanceSquared(new Vector2(point.X, point.Y), target) < Vector2.DistanceSquared(new Vector2(closest.X, closest.Y), target))
                {
                    closest = point;
                }
                y -= 3;
            }
            return closest;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target, UnitTypes unitType)
        {
            // main building
            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                RoomForExitingUnits(x, y, size, unitType) &&
                !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasAnyCreep(x, y, size / 2f) &&
                (mineralFields == null || !mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16)) &&
                (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)) &&
                BuildingService.RoomBelowAndAbove(x, y, size) &&
                BuildingService.RoomForAddonsOnOtherBuildings(x, y, size))
            {
                if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT)
                {
                    // the addon
                    var addonY = y - .5f;
                    var addonX = x + 2.5f;
                    var addonVector = new Vector2(addonX, addonY);
                    if (addonX >= 0 && addonY >= 0 && addonX < MapDataService.MapData.MapWidth && addonY < MapDataService.MapData.MapHeight &&
                        MapDataService.MapHeight((int)addonX, (int)addonY) == baseHeight &&
                        BuildingService.AreaBuildable(addonX, addonY, size / 2.0f) &&
                        !BuildingService.Blocked(addonX, addonY, size / 2.0f, -.5f) && !BuildingService.HasAnyCreep(addonX, addonY, size / 2f) &&
                        (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), addonVector) < 25)))
                    {
                        if (!BuildingService.BlocksResourceCenter(x, y, size / 2f) && !BuildingService.BlocksResourceCenter(addonX, addonY, size / 2f))
                        {
                            return new Point2D { X = x, Y = y };
                        }
                    }
                }
                else
                {
                    if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
                    {
                        if ((mineralFields == null || mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 64)) ||
                            (vespeneGeysers == null || vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 64)))
                        {
                            return null;
                        }
                    }
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }

        bool RoomForExitingUnits(float x, float y, float size, UnitTypes unitType)
        {
            if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY)
            {
                return BuildingService.AreaBuildable(x, y, size);
            }
            return BuildingService.AreaBuildable(x, y, size / 2.0f);
        }
    }
}
