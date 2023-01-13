using System.Collections.Generic;
using System.Numerics;

namespace Sharky.Pathing
{
    public class PathData
    {
        public Vector2 StartPosition { get; set; }
        public Vector2 EndPosition { get; set; }
        public List<Vector2> Path { get; set; }
    }
}
