namespace Sharky.MicroControllers.Protoss
{
    public class ObserverMicroController : FlyingDetectorMicroController
    {
        public ObserverMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }
    }
}
