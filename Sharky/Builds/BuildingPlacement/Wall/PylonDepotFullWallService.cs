using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class PylonDepotFullWallService
    {
        ChokePointsService ChokePointsService;
        ChokePointService ChokePointService;

        public PylonDepotFullWallService(DefaultSharkyBot defaultSharkyBot)
        {
            ChokePointsService = defaultSharkyBot.ChokePointsService;
            ChokePointService = defaultSharkyBot.ChokePointService;
        }

        public List<Point2D> GetFullPylonDepotWallSpots(Point2D location, Point2D targetLocation)
        {
            var chokePoints = ChokePointsService.GetChokePoints(location, targetLocation, 0);
            var chokePoint = chokePoints.Good.FirstOrDefault();
            if (chokePoint != null && Vector2.DistanceSquared(chokePoint.Center, new Vector2(location.X, location.Y)) < 900)
            {
                var wallPoints = ChokePointService.GetWallOffPoints(chokePoint.Points);

                if (wallPoints != null)
                {
                    var wallCenter = new Vector2(wallPoints.Sum(p => p.X) / wallPoints.Count(), wallPoints.Sum(p => p.Y) / wallPoints.Count());

                    if (chokePoint.Center.X > wallCenter.X) // left to right
                    {
                        if (chokePoint.Center.Y < wallCenter.Y) // top to bottom
                        {
                            var baseX = wallPoints.Last().X;
                            var baseY = wallPoints.Last().Y;

                            return new List<Point2D> { new Point2D { X = baseX, Y = baseY + 1 }, new Point2D { X = baseX - 2, Y = baseY }, new Point2D { X = baseX - 3, Y = baseY - 2 } };
                        }
                        else // bottom to top
                        {
                            var baseX = wallPoints.First().X;
                            var baseY = wallPoints.First().Y;

                            return new List<Point2D> { new Point2D { X = baseX - 1, Y = baseY }, new Point2D { X = baseX, Y = baseY - 2 }, new Point2D { X = baseX + 2, Y = baseY - 3 } };
                        }
                    }
                    else // right to left
                    {
                        if (chokePoint.Center.Y < wallCenter.Y) // top to bottom
                        {
                            var baseX = wallPoints.Last().X;
                            var baseY = wallPoints.Last().Y;

                            return new List<Point2D> { new Point2D { X = baseX, Y = baseY + 1 }, new Point2D { X = baseX - 1, Y = baseY + 3 }, new Point2D { X = baseX - 3, Y = baseY + 4 } };
                        }
                        else // bottom to top
                        {
                            var baseX = wallPoints.First().X;
                            var baseY = wallPoints.First().Y;

                            return new List<Point2D> { new Point2D { X = baseX + 1, Y = baseY }, new Point2D { X = baseX + 3, Y = baseY + 1 }, new Point2D { X = baseX + 4, Y = baseY + 3 } };
                        }
                    }
                }
            }

            return null;
        }
    }
}
