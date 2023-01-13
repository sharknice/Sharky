using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{
    public class AttackPathingService
    {
        MapDataService MapDataService;

        public AttackPathingService(DefaultSharkyBot defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
        }


        public PathData GetNearestPath(Vector2 start, Vector2 end)
        {
            var startHeight = MapDataService.MapHeight(start);
            var endHeight = MapDataService.MapHeight(end);

            var startPaths = MapDataService.MapData.PathData.Where(data => data.Path.Any(p => Vector2.DistanceSquared(p, start) <= 25 && MapDataService.MapHeight(p) == startHeight));
            var best = startPaths.FirstOrDefault(data => data.Path.Any(p => Vector2.DistanceSquared(p, end) <= 25 && MapDataService.MapHeight(p) == endHeight));
            if (best == null)
            {
                best = startPaths.FirstOrDefault(data => data.Path.Any(p => Vector2.DistanceSquared(p, end) <= 50 && MapDataService.MapHeight(p) == endHeight));
                if (best == null)
                {
                    return null;
                }
            }

            List<Vector2> path;
            var pathStart = best.Path.OrderBy(p => Vector2.DistanceSquared(p, start)).FirstOrDefault(p => MapDataService.MapHeight(p) == startHeight);
            var pathEnd = best.Path.OrderBy(p => Vector2.DistanceSquared(p, end)).FirstOrDefault(p => MapDataService.MapHeight(p) == endHeight);

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
                if (commander.UnitRole == UnitRole.Leader && commander.UnitCalculation.NearbyAllies.Count(a => !a.Unit.IsFlying) > 5)
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
                commander.CurrentPath = GetNearestPath(commander.UnitCalculation.Position, target.ToVector2());
                commander.CurrentPathIndex = 0;
            }

            if (commander.CurrentPath != null && commander.CurrentPathIndex < commander.CurrentPath.Path.Count - 1 && commander.CurrentPath.EndPosition.X == target.X && commander.CurrentPath.EndPosition.Y == target.Y)
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, commander.CurrentPath.Path[commander.CurrentPathIndex]) < 4)
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
