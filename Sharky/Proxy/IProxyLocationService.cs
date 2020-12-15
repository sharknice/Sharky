using SC2APIProtocol;

namespace Sharky.Proxy
{
    interface IProxyLocationService
    {
        Point2D GetCliffProxyLocation();
        Point2D GetGroundProxyLocation();
    }
}
