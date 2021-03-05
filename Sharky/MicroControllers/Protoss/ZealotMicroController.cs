using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;

namespace Sharky.MicroControllers.Protoss
{
    public class ZealotMicroController : IndividualMicroController
    {
        public ZealotMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled) 
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool WeaponReady(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 5 || commander.UnitCalculation.Unit.WeaponCooldown > 15; // a zealot has 2 attacks, so we do this because after one attack the cooldown starts over instead of both
        }
    }
}
