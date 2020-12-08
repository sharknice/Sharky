using Sharky.Managers;
using Sharky.Pathing;

namespace Sharky.MicroControllers.Protoss
{
    public class ZealotMicroController : IndividualMicroController
    {
        public ZealotMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, IBaseManager baseManager, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled) 
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, baseManager, sharkyOptions, microPriority, groupUpEnabled)
        {

        }
    }
}
