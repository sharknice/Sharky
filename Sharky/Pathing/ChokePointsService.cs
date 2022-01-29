using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{
    public class ChokePointsService
    {
        IPathFinder PathFinder;
        ChokePointService ChokePointService;

        public ChokePointsService(IPathFinder pathFinder, ChokePointService chokePointService)
        {
            PathFinder = pathFinder;
            ChokePointService = chokePointService;
        }

        public ChokePoints GetChokePoints(Point2D start, Point2D end, int frame)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var chokePoints = new ChokePoints();
            chokePoints.Good = GeHighGroundChokePoints(start, end, frame);
            chokePoints.Bad = GeHighGroundChokePoints(end, start, frame);

            stopwatch.Stop();
            //System.Console.WriteLine($"Generated Chokepoints in {stopwatch.ElapsedMilliseconds} ms");

            return chokePoints;
        }

        private List<ChokePoint> GeHighGroundChokePoints(Point2D start, Point2D end, int frame)
        {
            float maxDistance = 1000;
            var currentX = start.X;
            var currentY = start.Y;
            var chokepoints = new List<ChokePoint>();
            bool complete = false;
            while (!complete)
            {
                var path = PathFinder.GetGroundPath(currentX, currentY, end.X, end.Y, frame);
                var chokePoint = ChokePointService.FindHighGroundChokePoint(path, maxDistance);
                if (chokePoint != null)
                {
                    var entireChoke = ChokePointService.GetEntireChokePoint(chokePoint);
                    var center = new Vector2(entireChoke.Sum(p => p.X) / entireChoke.Count(), entireChoke.Sum(p => p.Y) / entireChoke.Count());
                    chokepoints.Add(new ChokePoint { Center = center, Points = entireChoke });
                    currentX = chokePoint.X;
                    currentY = chokePoint.Y;
                }
                else
                {
                    complete = true;
                }
            }

            return ConsolidateChokePoints(chokepoints);
        }

        private List<ChokePoint> ConsolidateChokePoints(List<ChokePoint> chokePoints)
        {
            var consolidatedChokepoints = new List<ChokePoint>();

            foreach (var chokePoint in chokePoints)
            {
                var match = consolidatedChokepoints.FirstOrDefault(c => Vector2.DistanceSquared(chokePoint.Center, c.Center) < 36);
                if (match != null)
                {
                    consolidatedChokepoints.Remove(match);
                    var points = match.Points;
                    points.AddRange(chokePoint.Points);
                    consolidatedChokepoints.Add(new ChokePoint { Points = points, Center = new Vector2(points.Sum(p => p.X) / points.Count(), points.Sum(p => p.Y) / points.Count()) });
                }
                else
                {
                    consolidatedChokepoints.Add(chokePoint);
                }    
            }

            return consolidatedChokepoints;
        }
    }
}
