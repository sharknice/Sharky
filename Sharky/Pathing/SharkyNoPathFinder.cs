using System.Collections.Generic;
using System.Numerics;

namespace Sharky.Pathing
{
    public class SharkyNoPathFinder : IPathFinder
    {
        public SharkyNoPathFinder()
        {
        }

        public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            throw new System.NotImplementedException();
        }
    }
}
