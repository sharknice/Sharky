using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class VoidRayMicroController : IndividualMicroController
    {
        public VoidRayMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, sharkyOptions, microPriority, groupUpEnabled)
        {
        }

        protected override bool WeaponReady(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown == 0 || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK);
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            var dps = commander.UnitCalculation.Attackers.Sum(a => a.Dps);
            var hp = commander.UnitCalculation.Attackers.Sum(a => a.Unit.Health + a.Unit.Shield);
            if (dps <= 0 || hp <= 0) { return false; }

            var timeToLoseShield = commander.UnitCalculation.Unit.Shield / dps;
            var timeToKill = hp / commander.UnitCalculation.Dps;

            if (timeToLoseShield > timeToKill)
            {
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.AbilityOffCooldown(Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, frame, SharkyOptions.FramesPerSecond, UnitDataManager))
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK))
                {
                    foreach (var tag in commander.UnitCalculation.Unit.Orders.Select(o => o.TargetUnitTag))
                    {
                        UnitCalculation unit;
                        if (UnitManager.EnemyUnits.TryGetValue(tag, out unit))
                        {
                            if (unit.Attributes.Contains(Attribute.Armored))
                            {
                                if (commander.UnitCalculation.EnemiesInRange.Where(e => e.Attributes.Contains(Attribute.Armored)).Sum(e => e.Unit.Health) > 200)
                                {
                                    action = commander.Order(frame, Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
