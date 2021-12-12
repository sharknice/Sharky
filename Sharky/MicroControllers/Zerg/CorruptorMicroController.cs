using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Zerg
{
    public class CorruptorMicroController : IndividualMicroController
    {
        public CorruptorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var spraying = commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_CAUSTICSPRAY);
            if (spraying && commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.HealthMax)
            {
                return true;
            }

            if (commander.UnitCalculation.EnemiesInRange.Count() > 0 || commander.UnitCalculation.EnemiesInRangeOfAvoid.Count() > 0)
            {
                return false;
            }

            if (spraying)
            {
                return true;
            }

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_CAUSTICSPRAY, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            var building = GetBestBuildingTarget(commander.UnitCalculation.NearbyEnemies.Take(25).Where(e => e.Attributes.Contains(Attribute.Structure)), commander);

            if (building != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_CAUSTICSPRAY, targetTag: building.Unit.Tag);
                return true;
            }

            return false;
        }
    }
}
