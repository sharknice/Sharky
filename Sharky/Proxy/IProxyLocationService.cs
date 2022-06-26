using SC2APIProtocol;

namespace Sharky.Proxy
{
    public interface IProxyLocationService
    {
        Point2D GetCliffProxyLocation(float offsetDistance = 0);
        Point2D GetGroundProxyLocation(float offsetDistance = 0);
    }
}
