using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Proxy
{
    public class ProxyLocationService : IProxyLocationService
    {
        IBaseManager BaseManager;
        ITargetingManager TargetingManager;

        public ProxyLocationService(IBaseManager baseManager, ITargetingManager targetingManager)
        {
            BaseManager = baseManager;
            TargetingManager = targetingManager;
        }

        public Point2D GetCliffProxyLocation()
        {
            int proxyBase = 2;
            // TODO: specific maps are layed out different, need to change proxybase for those, need to use the walking distance, not the air distance

            var baseLocation = BaseManager.BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingManager.EnemyMainBasePoint.X, TargetingManager.EnemyMainBasePoint.Y))).Take(proxyBase).Last().Location;

            var angle = Math.Atan2(TargetingManager.EnemyMainBasePoint.Y - baseLocation.Y, baseLocation.X - TargetingManager.EnemyMainBasePoint.X);
            var x = 8 * Math.Cos(angle);
            var y = 8 * Math.Sin(angle);
            return new Point2D { X = baseLocation.X + (float)x, Y = baseLocation.Y - (float)y };
        }
    }
}
