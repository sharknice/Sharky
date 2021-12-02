using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;

namespace Sharky.MicroControllers.Zerg
{
    public class LocustMicroController : IndividualMicroController
    {
        public LocustMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var bestTarget = GetBestTarget(commander, target, frame);

            if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }

        public override List<Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }

        public override List<Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, null, frame);
        }

        public override List<Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }
    }
}
