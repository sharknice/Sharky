using Sharky;
using System.Collections.Generic;

namespace Sharky
{
    public class BaseData
    {
        public List<BaseLocation> BaseLocations { get; set; }
        public List<BaseLocation> SelfBases { get; set; }
        public BaseLocation MainBase { get; set; }
    }
}
