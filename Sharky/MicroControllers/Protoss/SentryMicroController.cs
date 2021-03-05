using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class SentryMicroController : IndividualMicroController
    {
        public SentryMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled) 
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
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
            if (GuardianShield(commander, frame, out action))
            {
                return true;
            }

            if (Hallucinate(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool GuardianShield(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.GUARDIANSHIELD) || commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOf.Count(e => e.Range > 1) > 3)
            {
                action = commander.Order(frame, Abilities.EFFECT_GUARDIANSHIELD);
                return true;
            }
            return false;
        }

        bool Hallucinate(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) > 3 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Detector)))
            {
                action = commander.Order(frame, Abilities.HALLUCINATION_ARCHON);
                return true;
            }
            return false;
        }
    }
}
