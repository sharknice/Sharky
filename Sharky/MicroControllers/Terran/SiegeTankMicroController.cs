using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class SiegeTankMicroController : IndividualMicroController
    {
        public SiegeTankMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var enemiesInSiegeRange = commander.UnitCalculation.NearbyEnemies.Where(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius) * (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius));
            if (enemiesInSiegeRange.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED) || enemiesInSiegeRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 50)
            {
                action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                return true;
            }
            
            return false;
        }
    }
}
