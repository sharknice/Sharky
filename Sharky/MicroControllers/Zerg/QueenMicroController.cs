using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Zerg
{
    public class QueenMicroController : IndividualMicroController
    {
        public QueenMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.Shield < 20)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (Transfuse(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool Transfuse(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            // TODO: transfuse target logic
            var transfuseTarget = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.BuildProgress >= 1 && a.Unit.Health < a.Unit.HealthMax / 2);
            if (transfuseTarget != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_TRANSFUSION, targetTag: transfuseTarget.Unit.Tag);
                return true;
            }
            return false;
        }
    }
}
