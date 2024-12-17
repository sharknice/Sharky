namespace Sharky.MicroControllers.Terran
{
    public class SiegeTankMicroController : IndividualMicroController
    {
        public SiegeTankMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            if (MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) >= MapDataService.MapHeight(defensivePoint))
            {
                return commander.Order(frame, Abilities.MORPH_SIEGEMODE);
            }
            return null;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
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

            if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED) && commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK))
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK) && Vector2.Distance(e.Position, commander.UnitCalculation.Position) < 16))
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    TagService.TagAbility("siege");
                    action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                    return true;
                }
            }

            var enemiesInSiegeRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying && 
                (e.Damage > 0 || Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 12 * 12) && // get a little bit closer to buildings
                Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius) * (13 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius));
            if (enemiesInSiegeRange.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK) || enemiesInSiegeRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 50)
            {
                var enemiesTooClose = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying && e.Damage > 0 &&
                    Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= (2 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius) * (2 + e.Unit.Radius + commander.UnitCalculation.Unit.Radius));

                if (enemiesTooClose.Count() > enemiesInSiegeRange.Count() - enemiesTooClose.Count()) { return false; }

                CameraManager.SetCamera(commander.UnitCalculation.Position);
                TagService.TagAbility("siege");
                action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                return true;
            }
            
            return false;
        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var unitToSupport = GetSupportTarget(commander, supportTargets, target, defensivePoint);

            if (unitToSupport == null)
            {
                return Attack(commander, target, defensivePoint, groupCenter, frame);
            }

            if (unitToSupport.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && !unitToSupport.UnitCalculation.EnemiesInRange.Any() && !unitToSupport.UnitCalculation.EnemiesInRangeOf.Any() && !commander.UnitCalculation.EnemiesInRange.Any() && !commander.UnitCalculation.EnemiesInRangeOf.Any())
            {
                var closestEnemy = unitToSupport.UnitCalculation.NearbyEnemies.OrderBy(e => Vector2.DistanceSquared(e.Position, unitToSupport.UnitCalculation.Position)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    if (Vector2.Distance(commander.UnitCalculation.Position, closestEnemy.Position) > Vector2.Distance(unitToSupport.UnitCalculation.Position, closestEnemy.Position))
                    {
                        return commander.Order(frame, Abilities.MOVE, target);
                    }
                }
            }

            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }
    }
}
