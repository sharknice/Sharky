using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class ZealotMicroController : IndividualMicroController
    {
        public ZealotMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesInRangeOf.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK))
            {
                commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                return AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action);
            }

            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Count() > 0 && commander.UnitCalculation.EnemiesInRangeOfAvoid.All(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Count() > 0 && commander.UnitCalculation.EnemiesInRangeOfAvoid.All(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 5 || commander.UnitCalculation.Unit.WeaponCooldown > 15; // a zealot has 2 attacks, so we do this because after one attack the cooldown starts over instead of both
        }
    }
}
