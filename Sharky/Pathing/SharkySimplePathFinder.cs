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
            var cells = MapDataService.GetCells(startX, startY, 1);
            var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 1);
            var best = cells.OrderBy(c => c.EnemyAirDpsInRange).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            throw new System.NotImplementedException();
        }
    }
}
