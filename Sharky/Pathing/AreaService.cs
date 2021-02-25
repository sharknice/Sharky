using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Pathing
{
    public class AreaService
    {
        MapDataService MapDataService;

        public AreaService(MapDataService mapDataService)
        {
            MapDataService = mapDataService;
        }

        public List<Point2D> GetTargetArea(Point2D point)
        {
            var points = new List<Point2D>();

            var startHeight = MapDataService.MapHeight(point);
            for (var x = -25; x < 25; x++)
            {
                for (var y = -25; y < 25; y++)
                {
                    if (x + point.X > 0 && x + point.X < MapDataService.MapData.MapWidth && y + point.Y > 0 && y + point.Y < MapDataService.MapData.MapHeight)
                    {
                        if (MapDataService.MapHeight(x + (int)point.X, y + (int)point.Y) == startHeight && MapDataService.PathWalkable(x + (int)point.X, y + (int)point.Y))
                        {
                            points.Add(new Point2D { X = x + (int)point.X, Y = y + (int)point.Y });
                        }
                    }
                }
            }

            return points;
        }

        public bool InArea(Point point, List<Point2D> area)
        {
            var x = (int)point.X;
            var y = (int)point.Y;
            return area.Any(p => p.X == x && p.Y == y);
        }
    }
}
