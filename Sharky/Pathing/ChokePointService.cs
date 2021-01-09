using SC2APIProtocol;
using System.Collections.Generic;
using System.Numerics;

namespace Sharky.Pathing
{
    public class ChokePointService
    {
        IPathFinder PathFinder;
        MapDataService MapDataService;

        public ChokePointService(IPathFinder pathFinder, MapDataService mapDataService)
        {
            PathFinder = pathFinder;
            MapDataService = mapDataService;
        }

        public Point2D FindDefensiveChokePoint(Point2D start, Point2D end, int frame, float maxDistance = 30f)
        {
            var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, frame);

            var chokePoint = FindHighGroundChokePoint(path, maxDistance);
            if (chokePoint != null)
            {
                return chokePoint;
            }

            chokePoint = FindFlatChokePoint(path, maxDistance);
            if (chokePoint != null)
            {
                return chokePoint;
            }

            return null;
        }


        public Point2D FindHighGroundChokePoint(List<Vector2> path, float maxDistance = 30f)
        {
            if (path.Count > 0)
            {
                var startHeight = MapDataService.MapHeight(new Point2D { X = path[0].X, Y = path[0].Y });
                var previousPoint = path[0];

                foreach (var point in path)
                {
                    if (startHeight > MapDataService.MapHeight(new Point2D { X = point.X, Y = point.Y }))
                    {
                        if (Vector2.DistanceSquared(path[0], point) > maxDistance * maxDistance)
                        {
                            return null;
                        }
                        return new Point2D { X = previousPoint.X, Y = previousPoint.Y };
                    }
                    previousPoint = point;
                }
            }

            return null;
        }

        public Point2D FindFlatChokePoint(List<Vector2> path, float maxDistance = 15)
        {
            return null;
            // TODO: check points around if they are walkable, if you can fit a circle near, not sur ehow to do it exactly
            if (path.Count > 0)
            {
                var previousPoint = path[0];

                foreach (var point in path)
                {
                    //if (IsFlatChoke(new Point2D { X = point.X, Y = point.Y }))
                    {
                        if (Vector2.DistanceSquared(path[0], point) > maxDistance * maxDistance)
                        {
                            return null;
                        }
                        return new Point2D { X = previousPoint.X, Y = previousPoint.Y };
                    }
                    previousPoint = point;
                }
            }

            return null;
        }
    }
}
