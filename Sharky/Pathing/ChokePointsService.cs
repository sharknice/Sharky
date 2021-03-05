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

            //var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, frame);
            //float maxDistance = 1000;
            //var currentX = start.X;
            //var currentY = start.Y;
            //bool done = false;
            //while (!done)
            //{
            //    var goodChokePoint = ChokePointService.FindHighGroundChokePoint(path, maxDistance);
            //    var goodLength = maxDistance;
            //    if (goodChokePoint != null)
            //    {
            //        goodLength = PathFinder.GetGroundPath(currentX, currentY, goodChokePoint.X, goodChokePoint.Y, frame).Count;
            //    }

            //    var neutralChokePoint = ChokePointService.FindFlatChokePoint(path, maxDistance);
            //    var neutralLength = maxDistance;
            //    if (neutralChokePoint != null)
            //    {
            //        neutralLength = PathFinder.GetGroundPath(currentX, currentY, neutralChokePoint.X, neutralChokePoint.Y, frame).Count;
            //    }

            //    var badChokePoint = ChokePointService.FindLowGroundChokePoint(path, maxDistance);
            //    var badLength = maxDistance;
            //    if (badChokePoint != null)
            //    {
            //        badLength = PathFinder.GetGroundPath(currentX, currentY, badChokePoint.X, badChokePoint.Y, frame).Count;
            //    }

            //    if (goodLength < maxDistance && goodLength < neutralLength && goodLength < badLength)
            //    {
            //        currentX = goodChokePoint.X;
            //        currentY = goodChokePoint.Y;
            //        var entireChoke = ChokePointService.GetEntireChokePoint(goodChokePoint);
            //        var center = new Vector2(entireChoke.Sum(p => p.X) / entireChoke.Count(), entireChoke.Sum(p => p.Y) / entireChoke.Count());
            //        chokePoints.Good.Add(new ChokePoint { Center = center, Points = entireChoke });
            //    }
            //    else if (neutralLength < maxDistance && neutralLength < goodLength && neutralLength < badLength)
            //    {
            //        currentX = neutralChokePoint.X;
            //        currentY = neutralChokePoint.Y;
            //        var entireChoke = ChokePointService.GetEntireChokePoint(neutralChokePoint);
            //        var center = new Vector2(entireChoke.Sum(p => p.X) / entireChoke.Count(), entireChoke.Sum(p => p.Y) / entireChoke.Count());
            //        chokePoints.Neutral.Add(new ChokePoint { Center = center, Points = entireChoke });
            //    }
            //    else if (badLength < maxDistance && badLength < neutralLength && badLength < goodLength)
            //    {
            //        currentX = badChokePoint.X;
            //        currentY = badChokePoint.Y;
            //        var entireChoke = ChokePointService.GetEntireChokePoint(badChokePoint);
            //        var center = new Vector2(entireChoke.Sum(p => p.X) / entireChoke.Count(), entireChoke.Sum(p => p.Y) / entireChoke.Count());
            //        chokePoints.Bad.Add(new ChokePoint { Center = center, Points = entireChoke });
            //    }
            //    else
            //    {
            //        done = true;
            //    }

            //    path = PathFinder.GetGroundPath(currentX, currentY, end.X, end.Y, frame);
            //    if (path.Count > 10)
            //    {
            //        path = path.Skip(10).ToList();
            //    }
            //    else
            //    {
            //        done = true;
            //    }
            //}

            stopwatch.Stop();
            System.Console.WriteLine($"Generated Chokepoints in {stopwatch.ElapsedMilliseconds} ms");

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
