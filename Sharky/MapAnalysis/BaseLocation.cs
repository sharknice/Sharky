using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MapAnalysis
{
    public class BaseLocation
    {
        public List<MineralField> MineralFields { get; internal set; } = new List<MineralField>();
        public List<Gas> Gasses { get; internal set; } = new List<Gas>();
        public Point2D Pos { get; set; }
    }
}
