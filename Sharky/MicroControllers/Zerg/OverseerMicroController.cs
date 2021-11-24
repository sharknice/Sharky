using Sharky.DefaultBot;
using Sharky.Pathing;

namespace Sharky.MicroControllers.Protoss
{
    public class OverseerMicroController : FlyingDetectorMicroController
    {
        public OverseerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }
    }
}
