using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class LurkerBurrowedMicroController : IndividualMicroController
    {
        public LurkerBurrowedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<Action> action)
        {
            var lurkerRange = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.LURKERRANGE) ? 10 : 8;

            var groundEnemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying &&
                (Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < lurkerRange * lurkerRange));

            if (groundEnemiesInRange.Count() == 0)
            {
                action = commander.Order(frame, Abilities.BURROWUP_LURKER);
                return true;
            }

            action = null;
            return false;
        }
    }
}
