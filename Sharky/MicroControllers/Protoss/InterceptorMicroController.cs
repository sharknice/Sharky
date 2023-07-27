namespace Sharky.MicroControllers.Protoss
{
    public class InterceptorMicroController : IndividualMicroController
    {
        public InterceptorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, pathFinder, microPriority, groupUpEnabled)
        {
        }

        public override List<SC2Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return null;
        }
        public override List<SC2Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return null;
        }
        public override List<SC2Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return null;
        }
        public override List<SC2Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            return null;
        }
    }
}
