using SC2APIProtocol;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    public class MarineMicroController : IndividualMicroController
    {
        public MarineMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.STIMPACK))
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK)) // don't double stim
                {
                    return false;
                }

                if (commander.UnitCalculation.EnemiesInRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 100) // stim if more than 100 hitpoints in range
                {
                    action = commander.Order(frame, Abilities.EFFECT_STIM);
                    return true;
                }
            }

            return false;
        }
    }
}
