﻿namespace Sharky.Builds.BuildingPlacement
{
    public class TerranProductionGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;

        List<Point2D> LastLocations;
        List<Point2D> LastLocationsAddons;

        public TerranProductionGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;

            LastLocations = new List<Point2D>();
            LastLocationsAddons = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, float size, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases.Where(b => b.Location.X == BaseData.BaseLocations.FirstOrDefault().Location.X && b.Location.Y == BaseData.BaseLocations.FirstOrDefault().Location.Y))
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
                    if (unitType == UnitTypes.TERRAN_BARRACKSREACTOR || unitType == UnitTypes.TERRAN_BARRACKSTECHLAB || unitType == UnitTypes.TERRAN_FACTORYREACTOR || unitType == UnitTypes.TERRAN_FACTORYTECHLAB || unitType == UnitTypes.TERRAN_STARPORTREACTOR || unitType == UnitTypes.TERRAN_STARPORTTECHLAB)
                    {
                        LastLocationsAddons.Add(closest);
                        if (LastLocationsAddons.Count() > 5)
                        {
                            LastLocationsAddons.RemoveAt(0);
                        }
                    }
                    else
                    {
                        LastLocations.Add(closest);
                        if (LastLocations.Count() > 5)
                        {
                            LastLocations.RemoveAt(0);
                        }
                    }

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
            if (unitType == UnitTypes.TERRAN_BARRACKSREACTOR || unitType == UnitTypes.TERRAN_BARRACKSTECHLAB || unitType == UnitTypes.TERRAN_FACTORYREACTOR || unitType == UnitTypes.TERRAN_FACTORYTECHLAB || unitType == UnitTypes.TERRAN_STARPORTREACTOR || unitType == UnitTypes.TERRAN_STARPORTTECHLAB)
            {
                if (LastLocationsAddons.Any(l => l.X == x && l.Y == y))
                {
                    return null;
                }
            }
            else
            {
                if (LastLocations.Any(l => l.X == x && l.Y == y))
                {
                    return null;
                }
            }


            // main building
            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight && MapDataService.MapHeight((int)x - (int)(size / 2), (int)y) == baseHeight && MapDataService.MapHeight((int)x, (int)y - (int)(size / 2)) == baseHeight && MapDataService.MapHeight((int)x, (int)y + (int)(size / 2)) == baseHeight && MapDataService.MapHeight((int)x + (int)(size / 2), (int)y) == baseHeight &&
                RoomForExitingUnits(x, y, size, unitType) &&
                !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasAnyCreep(x, y, size / 2f) &&
                (mineralFields == null || !mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16)) &&
                (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)))
            {
                if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT)
                {
                    if (!BuildingService.RoomBelowAndAbove(x, y, size) || !BuildingService.RoomForAddonsOnOtherBuildings(x, y, size))
                    {
                        return null;
                    }

                    // the addon
                    var addonY = y - .5f;
                    var addonX = x + 2.5f;
                    var addonVector = new Vector2(addonX, addonY);
                    if (addonX >= 0 && addonY >= 0 && addonX < MapDataService.MapData.MapWidth && addonY < MapDataService.MapData.MapHeight &&
                        MapDataService.MapHeight((int)addonX, (int)addonY) == baseHeight &&
                        BuildingService.AreaBuildable(addonX, addonY, 1 / 2f) &&
                        !BuildingService.Blocked(addonX, addonY, 1 / 2.0f, -.5f) && !BuildingService.HasAnyCreep(addonX, addonY, 1 / 2f))
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
                    else if (!BuildingService.RoomBelowAndAbove(x, y, size) || !BuildingService.RoomForAddonsOnOtherBuildings(x, y, size))
                    {
                        return null;
                    }
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }

        bool RoomForExitingUnits(float x, float y, float size, UnitTypes unitType)
        {
            if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT)
            {
                return BuildingService.AreaBuildable(x, y, size + 4) && BuildingService.AreaBuildable(x + 2.5f, y -.5f, size + 4) && BuildingService.SameHeight(x, y, size) && BuildingService.SameHeight(x + 2.5f, y - .5f, size);
            }
            return BuildingService.AreaBuildable(x, y, size + 4);
        }
    }
}
