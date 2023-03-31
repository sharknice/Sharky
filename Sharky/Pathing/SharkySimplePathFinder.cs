using Roy_T.AStar.Paths;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{
    public class SharkySimplePathFinder : IPathFinder
    {
        MapDataService MapDataService;

        public SharkySimplePathFinder(MapDataService mapDataService)
        {
            MapDataService = mapDataService;
        }

        public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var end = new Vector2(endX, endY);
            var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var end = new Vector2(endX, endY);
            var best = cells.OrderBy(c => c.EnemyAirDpsInRange).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, PathFinder pathFinder = null)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var best = cells.Where(c => c.Walkable).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var end = new Vector2(endX, endY);
            var best = cells.Where(c => c.Walkable).OrderBy(c => c.InEnemyDetection).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, float radius)
        {
            return GetGroundPath(startX, startY, endX, endY, frame);
        }
    }
}
