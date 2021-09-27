using SC2APIProtocol;
using Sharky.Pathing;
using System;
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
        public Point2D FindPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                // grid placement for production and tech, put tech in same spot a production building would go
                // startX -1, startY +4, X +7/-7, Y +3/-3
                var targetVector = new Vector2(target.X, target.Y);
                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var mineralLocationVector = new Vector2(selfBase.MineralLineLocation.X, selfBase.MineralLineLocation.Y);
                var xStart = selfBase.Location.X - 1f;
                var yStart = selfBase.Location.Y + 4f;

                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, mineralLocationVector, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector);
                    if (point != null) { return point; }
                    x += 7;
                }
                x = xStart - 7;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, mineralLocationVector, yStart, selfBase.MineralFields, selfBase.VespeneGeysers, maxDistance, targetVector);
                    if (point != null) { return point; }
                    x -= 7;
                }
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, Vector2 mineralLocationVector, float yStart, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target)
        {
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralLocationVector, mineralFields, vespeneGeysers, maxDistance, target);
                if (point != null) { return point; }
                y += 3;
            }
            y = yStart -4;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralLocationVector, mineralFields, vespeneGeysers, maxDistance, target);
                if (point != null) { return point; }
                y -= 3;
            }
            return null;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, Vector2 mineralLocationVector, IEnumerable<Unit> mineralFields, List<Unit> vespeneGeysers, float maxDistance, Vector2 target)
        {
            // main building
            var vector = new Vector2(x, y);
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                (Vector2.DistanceSquared(vector, target) < (maxDistance * maxDistance)) &&
                MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                BuildingService.AreaBuildable(x, y, size / 2.0f) &&
                !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasCreep(x, y, size / 2.0f) &&
                (mineralFields == null || !mineralFields.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 16)) &&
                (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), vector) < 25)))
            {
                // the addon
                var addonY = y - .5f;
                var addonX = x + 2.5f;
                var addonVector = new Vector2(addonX, addonY);
                if (addonX >= 0 && addonY >= 0 && addonX < MapDataService.MapData.MapWidth && addonY < MapDataService.MapData.MapHeight &&
                    MapDataService.MapHeight((int)addonX, (int)addonY) == baseHeight &&
                    BuildingService.AreaBuildable(addonX, addonY, size / 2.0f) &&
                    !BuildingService.Blocked(addonX, addonY, size / 2.0f, -.5f) && !BuildingService.HasCreep(addonX, addonY, size / 2.0f) &&
                    (vespeneGeysers == null || !vespeneGeysers.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), addonVector) < 25)))
                {
                    return new Point2D { X = x, Y = y };
                }
            }

            return null;
        }
    }
}
