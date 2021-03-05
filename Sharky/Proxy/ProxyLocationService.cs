using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Proxy
{
    public class ProxyLocationService : IProxyLocationService
    {
        BaseData BaseData;
        TargetingData TargetingData;
        IPathFinder PathFinder;
        MapDataService MapDataService;
        AreaService AreaService;

        public ProxyLocationService(BaseData baseData, TargetingData targetingData, IPathFinder pathFinder, MapDataService mapDataService, AreaService areaService)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            PathFinder = pathFinder;
            MapDataService = mapDataService;
            AreaService = areaService;
        }

        public Point2D GetCliffProxyLocation()
        {
            var numberOfCloseLocations = NumberOfCloseBaseLocations();
            var closeAirLocations = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), new Vector2(b.Location.X, b.Location.Y))).Take(numberOfCloseLocations);

            var baseLocation = closeAirLocations.OrderBy(b => PathFinder.GetGroundPath(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y, b.Location.X, b.Location.Y, 0).Count()).Last().Location;

            var angle = Math.Atan2(TargetingData.EnemyMainBasePoint.Y - baseLocation.Y, baseLocation.X - TargetingData.EnemyMainBasePoint.X);
            var x = 0 * Math.Cos(angle);
            var y = 0 * Math.Sin(angle);
            var location = new Point2D { X = baseLocation.X + (float)x, Y = baseLocation.Y - (float)y };
            if (MapDataService.PathWalkable(location))
            {
                return location;
            }
            return baseLocation;
        }

        public Point2D GetGroundProxyLocation()
        {
            int proxyBase = NumberOfCloseBaseLocations() + 1;
            var orderedLocations = BaseData.BaseLocations.OrderBy(b => PathFinder.GetGroundPath(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y, b.Location.X, b.Location.Y, 0).Count());

            var baseLocation = orderedLocations.Take(proxyBase).Last().Location;

            var angle = Math.Atan2(TargetingData.EnemyMainBasePoint.Y - baseLocation.Y, baseLocation.X - TargetingData.EnemyMainBasePoint.X);
            var x = 6 * Math.Cos(angle);
            var y = 6 * Math.Sin(angle);
            var location = new Point2D { X = baseLocation.X + (float)x, Y = baseLocation.Y - (float)y };
            if (MapDataService.PathWalkable(location))
            {
                return location;
            }
            return baseLocation;
        }

        private int NumberOfCloseBaseLocations()
        {
            return BaseData.BaseLocations.Count(b => Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), new Vector2(b.Location.X, b.Location.Y)) < 2000);
        }

        public CliffProxyData GetCliffProxyData()
        {
            var outsideProxyLocation = GetCliffProxyLocation();
            var targetLocation = TargetingData.EnemyMainBasePoint;

            var angle = Math.Atan2(targetLocation.Y - outsideProxyLocation.Y, outsideProxyLocation.X - targetLocation.X);
            var x = -6 * Math.Cos(angle);
            var y = -6 * Math.Sin(angle);
            var loadingLocation = new Point2D { X = outsideProxyLocation.X + (float)x, Y = outsideProxyLocation.Y - (float)y };

            var loadingVector = new Vector2(loadingLocation.X, loadingLocation.Y);
            var DropArea = AreaService.GetTargetArea(targetLocation);
            var dropVector = DropArea.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), loadingVector)).First();
            x = -2 * Math.Cos(angle);
            y = -2 * Math.Sin(angle);
            var dropLocation = new Point2D { X = dropVector.X + (float)x, Y = dropVector.Y - (float)y };

            return new CliffProxyData { OutsideProxyLocation = outsideProxyLocation, TargetLocation = targetLocation, LoadingLocation = loadingLocation, DropLocation = dropLocation };
        }
    }
}
