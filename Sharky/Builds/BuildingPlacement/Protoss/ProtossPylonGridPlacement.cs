using SC2APIProtocol;
using Sharky.Pathing;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossPylonGridPlacement
    {
        BaseData BaseData;
        MapDataService MapDataService;
        DebugService DebugService;
        BuildingService BuildingService;   

        public ProtossPylonGridPlacement(BaseData baseData, MapDataService mapDataService, DebugService debugService, BuildingService buildingService)
        {
            BaseData = baseData;

            MapDataService = mapDataService;
            DebugService = debugService;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, float maxDistance, float minimumMineralProximinity)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                var baseHeight = MapDataService.MapHeight(selfBase.Location);
                var mineralLocationVector = new Vector2(selfBase.MineralLineLocation.X, selfBase.MineralLineLocation.Y);
                var xStart = selfBase.Location.X + .5f;
                var yStart = selfBase.Location.Y + 8.5f;

                var x = xStart;
                while (x - xStart < 30)
                {
                    var point = GetValidPointInColumn(x, baseHeight, mineralLocationVector, yStart, maxDistance, target);
                    if (point != null) { return point; }
                    x += 10;
                }
                x = xStart - 10;
                while (xStart - x < 30)
                {
                    var point = GetValidPointInColumn(x, baseHeight, mineralLocationVector, yStart, maxDistance, target);
                    if (point != null) { return point; }
                    x -= 10;
                }
            }

            return null;
        }

        Point2D GetValidPointInColumn(float x, int baseHeight, Vector2 mineralLocationVector, float yStart, float maxDistance, Point2D target)
        {
            var y = yStart;
            while (y - yStart < 30)
            {
                var point = GetValidPoint(x, y, baseHeight, mineralLocationVector, maxDistance, target);
                if (point != null) { return point; }
                var point2 = GetValidPoint(x - 3, y - 1, baseHeight, mineralLocationVector, maxDistance, target);
                if (point2 != null) { return point2; }
                y += 10;
            }
            y = yStart - 10;
            while (yStart - y < 30)
            {
                var point = GetValidPoint(x, y, baseHeight, mineralLocationVector, maxDistance, target);
                if (point != null) { return point; }
                var point2 = GetValidPoint(x - 3, y - 1, baseHeight, mineralLocationVector, maxDistance, target);
                if (point2 != null) { return point2; }
                y -= 10;
            }
            return null;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, Vector2 mineralLocationVector, float maxDistance, Point2D target)
        {
            var size = 2.25f;
            if (Vector2.DistanceSquared(new Vector2(x, y), mineralLocationVector) > 36 && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(target.X, target.Y)) < maxDistance * maxDistance)
            {
                if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight &&
                    MapDataService.MapHeight((int)x, (int)y) == baseHeight &&
                    BuildingService.AreaBuildable(x, y, size / 2.0f) &&
                    !BuildingService.Blocked(x, y, size / 2.0f, 0f) && !BuildingService.HasAnyCreep(x, y, size / 2.0f) && !BuildingService.BlocksResourceCenter(x, y, size / 2.0f))
                {
                    return new Point2D { X = x, Y = y };
                }
            }
            return null;
        }
    }
}
