using System.Collections.Generic;
using System.Numerics;

namespace Sharky.Pathing
{
    public interface IPathFinder
    {
        IEnumerable<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame);
        IEnumerable<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame);
        IEnumerable<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame);
    }
}
