using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Linq;

namespace Sharky.Proxy
{
    public class ProxyLocationService : IProxyLocationService
    {
        IBaseManager BaseManager;
        ITargetingManager TargetingManager;
        IPathFinder PathFinder;

        public ProxyLocationService(IBaseManager baseManager, ITargetingManager targetingManager, IPathFinder pathFinder)
        {
            BaseManager = baseManager;
            TargetingManager = targetingManager;
            PathFinder = pathFinder;
        }

        public Point2D GetCliffProxyLocation()
        {
            int proxyBase = 3;
            // TODO: specific maps are layed out different, need to change proxybase for those, need to use the walking distance, not the air distance

            var orderedLocations = BaseManager.BaseLocations.OrderBy(b => PathFinder.GetGroundPath(TargetingManager.EnemyMainBasePoint.X, TargetingManager.EnemyMainBasePoint.Y, b.Location.X, b.Location.Y, 0).Count());
            var baseLocation = orderedLocations.Take(proxyBase).Last().Location;

            var angle = Math.Atan2(TargetingManager.EnemyMainBasePoint.Y - baseLocation.Y, baseLocation.X - TargetingManager.EnemyMainBasePoint.X);
            var x = 8 * Math.Cos(angle);
            var y = 8 * Math.Sin(angle);
            return new Point2D { X = baseLocation.X + (float)x, Y = baseLocation.Y - (float)y };
        }
    }
}
