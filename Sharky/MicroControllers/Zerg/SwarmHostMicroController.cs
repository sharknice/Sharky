namespace Sharky.MicroControllers.Zerg
{
    public class SwarmHostMicroController : IndividualMicroController
    {
        public SwarmHostMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_SPAWNLOCUSTS, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyEnemies.Any() || commander.UnitCalculation.NearbyAllies.Count() > 10 || Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y)) < 1600)
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                TagService.TagAbility("locust");
                action = commander.Order(frame, Abilities.EFFECT_SPAWNLOCUSTS, TargetingData.AttackPoint);
                return true;
            }

            return false;
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2Action> actions = null;

            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out actions))
            {
                return actions;
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }

        protected override bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var range = 20;
            var enemiesInRange = new List<UnitCalculation>();

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            }

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(commander, closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;

            if (commander.UnitCalculation.IsOnCreep)
            {
                speed *= 1.3f;
            }
            return speed;
        }
    }
}
