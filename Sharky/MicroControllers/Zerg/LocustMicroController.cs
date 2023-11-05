namespace Sharky.MicroControllers.Zerg
{
    public class LocustMicroController : IndividualMicroController
    {
        public LocustMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        public override List<SC2Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2Action> action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.HasTargetUnitTag))
            {
                return action;
            }

            var bestTarget = GetBestTarget(commander, target, frame);

            if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public override List<SC2Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }

        public override List<SC2Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        public override List<SC2Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, null, frame);
        }

        public override List<SC2Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;

            if (commander.UnitCalculation.IsOnCreep)
            {
                speed *= 1.4f;
            }
            return speed;
        }
    }
}
