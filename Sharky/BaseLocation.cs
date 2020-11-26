using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky
{
    public class BaseLocation
    {
        public BaseLocation()
        {
            MineralFields = new List<Unit>();
            VespeneGeysers = new List<Unit>();
        }

        public List<Unit> MineralFields { get; set; }
        public Point2D MineralLineLocation { get; set; }
        public List<Unit> VespeneGeysers { get; set; }
        public Point2D Location { get; set; }
        public Unit ResourceCenter { get; set; }
        public int MineralLineDefenseUnbuildableFrame { get; set; }
    }
}
