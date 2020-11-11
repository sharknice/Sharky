using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Sharky.Pathing
{
    public interface IPathFinder
    {
        IEnumerable<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame);
        IEnumerable<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame);

    }
}
