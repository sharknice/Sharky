namespace Sharky.Proxy
{
    public interface IProxyLocationService
    {
        Point2D GetCliffProxyLocation(float offsetDistance = 0);
        Point2D GetGroundProxyLocation(float offsetDistance = 0);
        Point2D GetFurthestCliffProxyLocation(float offsetDistance = 0);
        Point2D GetClosestCliffProxyLocation(float offsetDistance = 0);

        ProxyData? GetProxyData();
    }
}
