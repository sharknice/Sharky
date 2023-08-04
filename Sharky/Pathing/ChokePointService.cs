namespace Sharky.Pathing
{
    public class ChokePointService
    {
        IPathFinder PathFinder;
        MapDataService MapDataService;
        BuildingService BuildingService;

        public ChokePointService(IPathFinder pathFinder, MapDataService mapDataService, BuildingService buildingService)
        {
            PathFinder = pathFinder;
            MapDataService = mapDataService;
            BuildingService = buildingService;
        }

        public List<Point2D> FindWallPoints(Point2D start, Point2D end, int frame, float maxDistance = 30f)
        {
            var chokePoint = FindDefensiveChokePoint(start, end, frame, maxDistance);
            if (chokePoint != null)
            {
                return GetEntireChokePoint(chokePoint);
            }
            return null;
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

        public Point2D FindLowGroundChokePoint(List<Vector2> path, float maxDistance = 30f)
        {
            if (path.Count > 0)
            {
                var startHeight = MapDataService.MapHeight(new Point2D { X = path[0].X, Y = path[0].Y });
                var previousPoint = path[0];

                foreach (var point in path)
                {
                    if (startHeight < MapDataService.MapHeight(new Point2D { X = point.X, Y = point.Y }))
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
            return null; // TODO: check points around if they are walkable, if you can fit a circle near, not sure how to do it exactly

            //if (path.Count > 0)
            //{
            //    var previousPoint = path[0];

            //    foreach (var point in path)
            //    {
            //        if (IsFlatChoke(new Point2D { X = point.X, Y = point.Y }))
            //        {
            //            if (Vector2.DistanceSquared(path[0], point) > maxDistance * maxDistance)
            //            {
            //                return null;
            //            }
            //            return new Point2D { X = previousPoint.X, Y = previousPoint.Y };
            //        }
            //        previousPoint = point;
            //    }
            //}

            //return null;
        }

        private bool IsFlatChoke(Point2D chokePoint)
        {
            // check area of map with radius of 10 around this, if percentage that is walkable and the same height is below certain amount it's a choke point
            var notChokeCount = 0;
            var startHeight = MapDataService.MapHeight(chokePoint);

            for (var x = -5; x < 10; x++)
            {
                for (var y = -5; y < 10; y++)
                {
                    if (MapDataService.MapHeight(x + (int)chokePoint.X, y + (int)chokePoint.Y) == startHeight && MapDataService.PathWalkable(x + (int)chokePoint.X, y + (int)chokePoint.Y))
                    {
                        if (!TouchingLowerPoint(x + (int)chokePoint.X, y + (int)chokePoint.Y, startHeight) || !TouchingUnwalkablePoint(x + (int)chokePoint.X, y + (int)chokePoint.Y, startHeight))
                        {
                            notChokeCount++;
                            if (notChokeCount > 50)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public List<Point2D> GetEntireChokePoint(Point2D chokePoint)
        {
            var chokePoints = new List<Point2D> { chokePoint };

            var startHeight = MapDataService.MapHeight(chokePoint);
            for (var x = -5; x < 10; x++)
            {
                for (var y = -5; y < 10; y++)
                {
                    if (MapDataService.MapHeight(x + (int)chokePoint.X, y + (int)chokePoint.Y) == startHeight && MapDataService.PathWalkable(x + (int)chokePoint.X, y + (int)chokePoint.Y))
                    {
                        // find if there is a touching point that is lower
                        if (TouchingLowerPoint(x + (int)chokePoint.X, y + (int)chokePoint.Y, startHeight))
                        {
                            chokePoints.Add(new Point2D { X = x + (int)chokePoint.X, Y = y + (int)chokePoint.Y });
                        }
                    }
                }
            }

            return chokePoints.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        public List<Point2D> GetEntireBottomOfRamp(Point2D chokePoint)
        {
            var chokePoints = new List<Point2D>();

            var startHeight = MapDataService.MapHeight(chokePoint);
            for (var x = -5; x < 10; x++)
            {
                for (var y = -5; y < 10; y++)
                {
                    if (MapDataService.MapHeight(x + (int)chokePoint.X, y + (int)chokePoint.Y) == startHeight && MapDataService.PathWalkable(x + (int)chokePoint.X, y + (int)chokePoint.Y))
                    {
                        // find if there is a touching point that is higher
                        if (TouchingHigherPoint(x + (int)chokePoint.X, y + (int)chokePoint.Y, startHeight))
                        {
                            chokePoints.Add(new Point2D { X = x + (int)chokePoint.X, Y = y + (int)chokePoint.Y });
                        }
                    }
                }
            }

            return chokePoints.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        public List<Point2D> GetWallOffPoints(List<Point2D> chokePoints)
        {
            var wallPoints = new List<Point2D>();

            foreach (var point in chokePoints)
            {
                if (BuildingService.AreaBuildable(point.X, point.Y, 1))
                {
                    wallPoints.Add(point);
                }
                else
                {
                    if (BuildingService.AreaBuildable(point.X, point.Y - 1, .1f)) { wallPoints.Add(point); }
                    if (BuildingService.AreaBuildable(point.X, point.Y + 1, .1f)) { wallPoints.Add(point); }

                    if (BuildingService.AreaBuildable(point.X - 1, point.Y, .1f)) { wallPoints.Add(point); }
                    if (BuildingService.AreaBuildable(point.X - 1, point.Y - 1, .1f)) { wallPoints.Add(point); }
                    if (BuildingService.AreaBuildable(point.X - 1, point.Y + 1, .1f)) { wallPoints.Add(point); }

                    if (BuildingService.AreaBuildable(point.X + 1, point.Y, .1f)) { wallPoints.Add(point); }
                    if (BuildingService.AreaBuildable(point.X + 1, point.Y - 1, .1f)) { wallPoints.Add(point); }
                    if (BuildingService.AreaBuildable(point.X + 1, point.Y + 1, .1f)) { wallPoints.Add(point); }
                }
            }

            return wallPoints.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        private bool TouchingHigherPoint(int x, int y, int startHeight)
        {
            if (MapDataService.MapHeight(x, y + 1) > startHeight && MapDataService.PathWalkable(x, y + 1))
            {
                return true;
            }
            if (MapDataService.MapHeight(x, y - 1) > startHeight && MapDataService.PathWalkable(x, y - 1))
            {
                return true;
            }
            if (MapDataService.MapHeight(x + 1, y) > startHeight && MapDataService.PathWalkable(x + 1, y))
            {
                return true;
            }
            if (MapDataService.MapHeight(x - 1, y) > startHeight && MapDataService.PathWalkable(x - 1, y))
            {
                return true;
            }
            return false;
        }

        private bool TouchingLowerPoint(int x, int y, int startHeight)
        {
            if (MapDataService.MapHeight(x, y + 1) < startHeight && MapDataService.PathWalkable(x, y + 1))
            {
                return true;
            }
            if (MapDataService.MapHeight(x, y - 1) < startHeight && MapDataService.PathWalkable(x, y - 1))
            {
                return true;
            }
            if (MapDataService.MapHeight(x + 1, y) < startHeight && MapDataService.PathWalkable(x + 1, y))
            {
                return true;
            }
            if (MapDataService.MapHeight(x - 1, y) < startHeight && MapDataService.PathWalkable(x - 1, y))
            {
                return true;
            }
            return false;
        }

        private bool TouchingUnwalkablePoint(int x, int y, int startHeight)
        {
            if (!MapDataService.PathWalkable(x, y + 1))
            {
                return true;
            }
            if (!MapDataService.PathWalkable(x, y - 1))
            {
                return true;
            }
            if (!MapDataService.PathWalkable(x + 1, y))
            {
                return true;
            }
            if (!MapDataService.PathWalkable(x - 1, y))
            {
                return true;
            }
            return false;
        }
    }
}
