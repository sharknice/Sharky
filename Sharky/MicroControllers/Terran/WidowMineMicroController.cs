namespace Sharky.MicroControllers.Terran
{
    public class WidowMineMicroController : IndividualMicroController
    {
        public WidowMineMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            if (MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) >= MapDataService.MapHeight(defensivePoint))
            {
                return commander.Order(frame, Abilities.BURROWDOWN_WIDOWMINE);
            }
            return null;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
            {
                action = commander.Order(frame, Abilities.BURROWDOWN_WIDOWMINE);
                return true;
            }
            
            return false;
        }
    }
}
