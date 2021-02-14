using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class HighTemplarMicroController : IndividualMicroController
    {
        private int StormRange = 9;
        private double StormRadiusSquared = 2.25;
        private double StormRadius = 1.5;
        private double FeedbackRangeSquared = 100;

        public HighTemplarMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled) 
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
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
            if (Storm(commander, frame, out action))
            {
                return true;
            }

            if (Feedback(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool Feedback(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            var vector = new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Energy > 1 && Vector2.DistanceSquared(new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y), vector) < FeedbackRangeSquared).OrderByDescending(e => e.Unit.Energy);

            var oneShotKill = enemiesInRange.Where(e => e.Unit.Energy * .5 > e.Unit.Health + e.Unit.Shield).FirstOrDefault();
            if (oneShotKill != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, oneShotKill.Unit.Tag);
                return true;
            }
            var target = enemiesInRange.FirstOrDefault();
            if (target != null && target.Unit.Energy > 50)
            {
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, target.Unit.Tag);
                return true;
            }

            return false;
        }

        bool Storm(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            // TODO: psistorm
            
            return false;
        }
    }
}
