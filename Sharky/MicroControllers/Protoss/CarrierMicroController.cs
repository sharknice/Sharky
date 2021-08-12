using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;


namespace Sharky.MicroControllers.Protoss
{
    public class CarrierMicroController : IndividualMicroController
    {
        public CarrierMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return true;
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            var interceptorCount = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR);
            var carrierCount = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_CARRIER) + 1;

            if (bestTarget != null && interceptorCount >= carrierCount * 8)
            {
                if (commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR && u.Unit.Orders.Any(o => o.TargetUnitTag == bestTarget.Unit.Tag)) >= 8)
                {
                    // move up to 14 range away from target
                    // move to target, avoid deceleration, etc.
                    action = null;
                    return false;
                }
            }

            return base.AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action);
        }
    }
}
