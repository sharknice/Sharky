namespace Sharky.MicroControllers.Terran
{
    public class SiegeTankMicroController : IndividualMicroController
    {
        int LastLeapFromSiegeFrame = -1000;

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

            // leap frog toward enemies
            if (LastLeapFromSiegeFrame + 120 < frame && commander.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying && !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK) && !commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                TagService.TagAbility("siege");
                action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                LastLeapFromSiegeFrame = frame;
                return true;
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

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying && e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy != null)
            {
                if (closestEnemy.PreviousUnitCalculation != null)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestEnemy.PreviousUnitCalculation.Position) > Vector2.DistanceSquared(commander.UnitCalculation.Position, closestEnemy.Position))
                    {
                        if (Vector2.Distance(closestEnemy.Position, commander.UnitCalculation.Position) < 25)
                        {
                            CameraManager.SetCamera(commander.UnitCalculation.Position);
                            TagService.TagAbility("siege");
                            action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                            return true;
                        }
                    }
                }
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
                var closestEnemy = unitToSupport.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying && e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)).OrderBy(e => Vector2.DistanceSquared(e.Position, unitToSupport.UnitCalculation.Position)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    if (closestEnemy.PreviousUnitCalculation != null)
                    {
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestEnemy.PreviousUnitCalculation.Position) > Vector2.DistanceSquared(commander.UnitCalculation.Position, closestEnemy.Position))
                        {
                            CameraManager.SetCamera(commander.UnitCalculation.Position);
                            TagService.TagAbility("siege");
                            action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                            return action;
                        }
                    }
                    if (Vector2.Distance(commander.UnitCalculation.Position, closestEnemy.Position) + 2 > Vector2.Distance(unitToSupport.UnitCalculation.Position, closestEnemy.Position))
                    {
                        return commander.Order(frame, Abilities.MOVE, closestEnemy.Position.ToPoint2D());
                    }
                    else
                    {
                        CameraManager.SetCamera(commander.UnitCalculation.Position);
                        TagService.TagAbility("siege");
                        action = commander.Order(frame, Abilities.MORPH_SIEGEMODE);
                        return action;
                    }
                }
            }

            if (unitToSupport.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && !commander.UnitCalculation.EnemiesInRange.Any() && !commander.UnitCalculation.EnemiesInRangeOf.Any() && !unitToSupport.UnitCalculation.EnemiesInRangeOf.Any())
            {
                if (OffensiveAbility(commander, target, defensivePoint, groupCenter, null, frame, out action)) { return action; }
                if (Vector2.Distance(commander.UnitCalculation.Position, target.ToVector2()) > Vector2.Distance(unitToSupport.UnitCalculation.Position, target.ToVector2()))
                {
                    var closestEnemy = unitToSupport.UnitCalculation.NearbyEnemies.OrderBy(e => Vector2.DistanceSquared(e.Position, unitToSupport.UnitCalculation.Position)).FirstOrDefault();
                    if (closestEnemy != null)
                    {
                        if (Vector2.Distance(commander.UnitCalculation.Position, closestEnemy.Position) > Vector2.Distance(unitToSupport.UnitCalculation.Position, closestEnemy.Position))
                        {
                            return commander.Order(frame, Abilities.MOVE, closestEnemy.Position.ToPoint2D());
                        }
                    }
                }
            }

            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }

        protected override bool CloseEnoughToSupportUnit(float distanceSquredToSupportUnit, UnitCommander unitToSupport)
        {
            if (unitToSupport.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED)
            {
                return false;
            }
            return base.CloseEnoughToSupportUnit(distanceSquredToSupportUnit, unitToSupport);
        }
    }
}
