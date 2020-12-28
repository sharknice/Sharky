using SC2APIProtocol;
using Sharky.Managers;
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

        public ProxyLocationService(BaseData baseData, TargetingData targetingData, IPathFinder pathFinder, MapDataService mapDataService)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            PathFinder = pathFinder;
            MapDataService = mapDataService;
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
            return BaseData.BaseLocations.Count(b => Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), new Vector2(b.Location.X, b.Location.Y)) < 1000);
        }
    }
}
