

namespace Sharky.MicroControllers.Protoss
{
    public class CarrierMicroController : IndividualMicroController
    {
        public CarrierMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            MaximumSupportDistanceSqaured = 9;
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

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) == 0 || Vector2.DistanceSquared(commander.UnitCalculation.Position, unitToSupport.UnitCalculation.Position) > 9)
            {
                return commander.Order(frame, Abilities.MOVE, new Point2D { X = unitToSupport.UnitCalculation.Position.X, Y = unitToSupport.UnitCalculation.Position.Y });          
            }

            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return true;
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_INTERCEPTORS))
            {
                if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    return Retreat(commander, target, defensivePoint, frame, out action);
                }
            }

            var interceptorCount = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR);
            var carrierCount = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_CARRIER) + 1;

            if (bestTarget != null && interceptorCount >= carrierCount * 8)
            {
                if (commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR && u.Unit.Orders.Any(o => o.TargetUnitTag == bestTarget.Unit.Tag)) >= 8)
                {
                    // move up to 14 range away from target
                    // move to target, avoid deceleration, etc.
                    action = null;
                    return false;
                }
            }

            return base.AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action);
        }

        protected override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.Take(25).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();

            if (commander.UnitCalculation.NearbyEnemies.Take(25).All(e => e.Range < commander.UnitCalculation.Range && e.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed))
            {
                if (closestEnemy != null && MapDataService.SelfVisible(closestEnemy.Unit.Pos))
                {
                    if (commander.UnitCalculation.Range > closestEnemy.Range)
                    {
                        var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed;
                        var enemySpeed = closestEnemy.UnitTypeData.MovementSpeed;
                        if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.MEDIVACSPEEDBOOST))
                        {
                            enemySpeed = 5.94f;
                        }
                        if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER))
                        {
                            enemySpeed += 1.57f;
                        }

                        if (speed > enemySpeed || closestEnemy.Range + 3 < commander.UnitCalculation.Range)
                        {
                            if (MaintainRange(commander, frame, out action)) { return true; }
                        }
                    }
                }
            }

            if (closestEnemy != null && commander.RetreatPathFrame + 1 < frame)
            {
                commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);

                commander.RetreatPathFrame = frame;
                commander.RetreatPathIndex = 1;
            }

            if (FollowPath(commander, frame, out action)) { return true; }

            action = commander.Order(frame, Abilities.MOVE, defensivePoint);
            return true;
        }
    }
}
