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

        // TODO: when retreating or defending if near own base need to seige up instead of attacking while unseiged

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            if (MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) >= MapDataService.MapHeight(defensivePoint))
            {
                return commander.Order(frame, Abilities.MORPH_SIEGEMODE);
            }
            return null;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (bestTarget != null && WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag)) { return false; }

            if (commander.UnitRole == UnitRole.Leader)
            {
                if (!commander.UnitCalculation.EnemiesThreateningDamage.Any() && commander.UnitCalculation.TargetPriorityCalculation.Overwhelm)
                {
                    return false;
                }
            }

            var friendlyDepots = commander.UnitCalculation.NearbyAllies.Take(25).Where(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            if (friendlyDepots.Any(depot => Vector2.DistanceSquared(depot.Position, commander.UnitCalculation.Position) < 4 )) 
            { 
                return false; 
            }

            var enemiesInSiegeRange = commander.UnitCalculation.NearbyEnemies.Take(25).Where(e => !e.Unit.IsFlying && 
                (e.Damage > 0 || Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 12 * 12) && // get a little bit closer to buildings
                Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius) * (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius));
            if (enemiesInSiegeRange.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED) || enemiesInSiegeRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 50)
            {
                var enemiesTooClose = commander.UnitCalculation.NearbyEnemies.Take(25).Where(e => !e.Unit.IsFlying && e.Damage > 0 &&
                    Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= (2 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius) * (2 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius));

                if (enemiesTooClose.Count() > enemiesInSiegeRange.Count() - enemiesTooClose.Count()) { return false; }

                action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                return true;
            }
            
            return false;
        }
    }
}
