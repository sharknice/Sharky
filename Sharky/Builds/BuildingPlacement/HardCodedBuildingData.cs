using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Builds.BuildingPlacement
{
    public class HardCodedBuildingData
    {
        public Point2D BasePosition { get; set; }
        public List<Point2D> Pylons { get; set; }
        public List<Point2D> Production { get; set; }
    }
}
