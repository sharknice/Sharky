namespace Sharky.Pathing
{
    public class AdvancedAttackPathingService : AttackPathingService
    {
        AreaService AreaService;
        public AdvancedAttackPathingService(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            AreaService = defaultSharkyBot.AreaService;
        }

        public override PathData GetNearestPath(Vector2 start, Vector2 end)
        {
            var startHeight = MapDataService.MapHeight(start);
            var endHeight = MapDataService.MapHeight(end);

            if (MapDataService.MapData.PathData == null) { return null; }

            var startPaths = MapDataService.MapData.PathData.Where(data => data.Path.Any(p => Vector2.DistanceSquared(p, start) <= 25 && MapDataService.MapHeight(p) == startHeight));
            var good = startPaths.Where(data => data.Path.Any(p => Vector2.DistanceSquared(p, end) <= 25 && MapDataService.MapHeight(p) == endHeight));

            var rated = good.OrderBy(p => GetRatedPathData(start, end, startHeight, endHeight, p).Item1);
            var best = rated.FirstOrDefault();
            if (best == null)
            {
                best = startPaths.FirstOrDefault(data => data.Path.Any(p => Vector2.DistanceSquared(p, end) <= 50 && MapDataService.MapHeight(p) == endHeight));
                if (best == null)
                {
                    return null;
                }
            }

            return GetPathData(start, end, startHeight, endHeight, best);
        }

        (float, PathData) GetRatedPathData(Vector2 start, Vector2 end, int startHeight, int endHeight, PathData p)
        {
            var pathData = GetPathData(start, end, startHeight, endHeight, p);
            var rating = pathData.Path.Sum(point => PointDifficulty(point));
            return (rating, pathData);
        }

        float PointDifficulty(Vector2 point) 
        {
            var heightScore = 5f;

            var height = MapDataService.MapHeight(point);
            var area = AreaService.GetAllArea(point.ToPoint2D(), 3);
            var closestHigher = area.Where(a => MapDataService.MapHeight(a) > height).OrderBy(a => Vector2.DistanceSquared(a.ToVector2(), point)).FirstOrDefault();
            if (closestHigher != null)
            {
                heightScore = 12f - Vector2.Distance(closestHigher.ToVector2(), point);
                return heightScore;
            }
            else
            {
                var closestLower = area.Where(a => MapDataService.MapHeight(a) < height).OrderBy(a => Vector2.DistanceSquared(a.ToVector2(), point)).FirstOrDefault();
                if (closestLower != null)
                {
                    heightScore = Vector2.Distance(closestLower.ToVector2(), point) - 6f;
                }
            }
            
            return heightScore;
        }
    }
}
