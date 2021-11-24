using SC2APIProtocol;

namespace Sharky.Proxy
{
    interface IProxyLocationService
    {
        Point2D GetCliffProxyLocation(float offsetDistance);
        Point2D GetGroundProxyLocation(float offsetDistance);
    }
}
