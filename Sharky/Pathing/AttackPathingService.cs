namespace Sharky.Pathing
{
    public class AttackPathingService
    {
        protected MapDataService MapDataService;

        public AttackPathingService(DefaultSharkyBot defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
        }


        public virtual PathData GetNearestPath(Vector2 start, Vector2 end)
        {
            var startHeight = MapDataService.MapHeight(start);
            var endHeight = MapDataService.MapHeight(end);

            if (MapDataService.MapData.PathData == null) { return null; }

            var startPaths = MapDataService.MapData.PathData.Where(data => data.Path.Any(p => Vector2.DistanceSquared(p, start) <= 25 && MapDataService.MapHeight(p) == startHeight) && data.Path.Any(p => Vector2.DistanceSquared(p, end) <= 25 && MapDataService.MapHeight(p) == endHeight));
            var best = startPaths.FirstOrDefault();
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

        protected PathData GetPathData(Vector2 start, Vector2 end, int startHeight, int endHeight, PathData best)
        {
            List<Vector2> path;
            var pathStart = best.Path.Where(p => MapDataService.MapHeight(p) == startHeight).OrderBy(p => Vector2.DistanceSquared(p, start)).FirstOrDefault();
            var pathEnd = best.Path.Where(p => MapDataService.MapHeight(p) == endHeight).OrderBy(p => Vector2.DistanceSquared(p, end)).FirstOrDefault();

            if (pathStart == Vector2.Zero || pathEnd == Vector2.Zero)
            {
                return null;
            }

            // return the list from start to end, path may need to be reversed
            var startIndex = best.Path.FindIndex(p => p == pathStart);
            var endIndex = best.Path.FindIndex(p => p == pathEnd);
            if (startIndex > endIndex)
            {
                // reverse order
                var reverse = best.Path.Skip(endIndex).Take(startIndex - endIndex);
                path = reverse.Reverse().ToList();
            }
            else
            {
                path = best.Path.Skip(startIndex).Take(endIndex - startIndex).ToList();
            }

            return new PathData { StartPosition = start, EndPosition = end, Path = path };
        }

        public Point2D GetNextPointToTarget(UnitCommander commander, Point2D target, int frame)
        {
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) < 100 && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(target))
            {
                commander.CurrentPath = null;
                return target;
            }

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
            {
                commander.CurrentPath = null;
                return target;
            }

            if (commander.UnitCalculation.Unit.IsFlying)
            {
                if (commander.UnitRole == UnitRole.Leader && commander.UnitCalculation.NearbyAllies.Count(a => !a.Unit.IsFlying) > 5 && commander.UnitCalculation.NearbyAllies.Count(a => a.Attributes.Contains(SC2Attribute.Structure)) < 4)
                {
                    // follow the ground path so supporting units can follow
                }
                else
                {
                    commander.CurrentPath = null;
                    return target;
                }
            }

            if (commander.CurrentPath == null || (int)commander.CurrentPath.EndPosition.X != (int)target.X || (int)commander.CurrentPath.EndPosition.Y != (int)target.Y || commander.CurrentPathIndex >= commander.CurrentPath.Path.Count || Vector2.DistanceSquared(commander.UnitCalculation.Position, commander.CurrentPath.Path[commander.CurrentPathIndex]) > 100)
            {
                if (commander.RetreatPathFrame + 20 < frame)
                {
                    commander.CurrentPath = GetNearestPath(commander.UnitCalculation.Position, target.ToVector2());
                    commander.CurrentPathIndex = 0;
                    commander.RetreatPathFrame = frame;
                }
            }

            if (commander.CurrentPath != null && commander.CurrentPathIndex < commander.CurrentPath.Path.Count - 1 && commander.CurrentPath.EndPosition.X == target.X && commander.CurrentPath.EndPosition.Y == target.Y)
            {
                var distance = 4;
                if (commander.UnitRole == UnitRole.Leader) { distance = 16; }
                if (commander.CommanderState == CommanderState.Stuck) { distance = 100; }
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, commander.CurrentPath.Path[commander.CurrentPathIndex]) < distance)
                {
                    commander.CurrentPathIndex++;
                    if (commander.CurrentPathIndex >= commander.CurrentPath.Path.Count)
                    {
                        return target;
                    }
                }
                return commander.CurrentPath.Path[commander.CurrentPathIndex].ToPoint2D();
            }

            return target;
        }
    }
}
