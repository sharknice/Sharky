using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranSupplyDepotGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;   

        public TerranSupplyDepotGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
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
                // X needs to be -3.5 from start, subtract or add 7
                // Y needs to be an even number

                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var mineralLocationVector = new Vector2(selfBase.MineralLineLocation.X, selfBase.MineralLineLocation.Y);
                var xStart = selfBase.Location.X - 3.5f;
                var yStart = FindYStart(selfBase.Location.Y);

                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, mineralLocationVector, yStart);
                    if (point != null) { return point; }
                    x += 7;
                }
                x = xStart - 7;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, size, baseHeight, mineralLocationVector, yStart);
                    if (point != null) { return point; }
                    x -= 7;
                }
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, float size, int baseHeight, Vector2 mineralLocationVector, float yStart)
        {
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralLocationVector);
                if (point != null) { return point; }
                y += 2;
            }
            y = yStart - 2;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, size, baseHeight, mineralLocationVector);
                if (point != null) { return point; }
                y -= 2;
            }
            return null;
        }

        Point2D GetValidPoint(float x, float y, float size, int baseHeight, Vector2 mineralLocationVector)
        {
            if (Vector2.DistanceSquared(new Vector2(x, y), mineralLocationVector) > 36)
            {
                if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                    MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                    BuildingService.AreaBuildable(x, y, size / 2.0f) &&
                    !BuildingService.Blocked(x, y, size / 2.0f, -.5f) && !BuildingService.HasAnyCreep(x, y, size / 2.0f))
                {
                    return new Point2D { X = x, Y = y };
                }
            }
            return null;
        }

        float FindYStart(float baseY)
        {
            var ceiling = (float)Math.Ceiling(baseY);
            if (ceiling % 2 == 0)
            {
                return ceiling;
            }
            else
            {
                return ceiling + 1;
            }
        }
    }
}
