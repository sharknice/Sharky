using Roy_T.AStar.Primitives;
using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{
    public class HarassPathingService
    {
        MapDataService MapDataService;
        BaseData BaseData;

        public HarassPathingService(DefaultSharkyBot defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
        }

        public PathData GetHomeToEnemyBaseAirPath(Point2D target)
        {
            var closestDistance = target.X - 0;
            var MidPoint = new Point2D { X = 0, Y = (target.Y + BaseData.SelfBases.FirstOrDefault().Location.Y) / 2f };
            var StagingPoint = new Point2D { X = 0, Y = target.Y };

            var right = MapDataService.MapData.MapWidth - target.X;
            if (right < closestDistance)
            {
                closestDistance = right;
                MidPoint = new Point2D { X = MapDataService.MapData.MapWidth, Y = (target.Y + BaseData.SelfBases.FirstOrDefault().Location.Y) / 2f };
                StagingPoint = new Point2D { X = MapDataService.MapData.MapWidth - 0, Y = target.Y };
            }

            var top = target.Y;
            if (top < closestDistance)
            {
                closestDistance = top;
                MidPoint = new Point2D { X = (target.X + BaseData.SelfBases.FirstOrDefault().Location.X) / 2f, Y = 0 };
                StagingPoint = new Point2D { X = target.X, Y = 0 };
            }

            var bottom = MapDataService.MapData.MapHeight - target.Y;
            if (bottom < closestDistance)
            {
                MidPoint = new Point2D { X = (target.X + BaseData.SelfBases.FirstOrDefault().Location.X) / 2f, Y = MapDataService.MapData.MapHeight };
                StagingPoint = new Point2D { X = target.X, Y = MapDataService.MapData.MapHeight - 0 };
            }

            var path = new List<Vector2> { MidPoint.ToVector2(), StagingPoint.ToVector2(), target.ToVector2() };

            return new PathData { StartPosition = path.First(), EndPosition = path.Last(), Path = path };
        }

        public Point2D GetNextPointToTarget(UnitCommander commander, Point2D target)
        {
            if (commander.CurrentPath == null || commander.CurrentPathIndex >= commander.CurrentPath.Path.Count) { return target; }

            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, commander.CurrentPath.Path[commander.CurrentPathIndex]) < 10)
            {
                commander.CurrentPathIndex++;
                if (commander.CurrentPathIndex >= commander.CurrentPath.Path.Count)
                {
                    commander.CurrentPath = null;
                    commander.CurrentPathIndex = 0;
                    return target;
                }
            }

            return commander.CurrentPath.Path[commander.CurrentPathIndex].ToPoint2D();
        }
    }
}
