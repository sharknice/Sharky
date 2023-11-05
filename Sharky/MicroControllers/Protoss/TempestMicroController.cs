namespace Sharky.MicroControllers.Protoss
{
    public class TempestMicroController : IndividualMicroController
    {
        public TempestMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive || MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 1)
            {
                return false;
            }

            var range = 12;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance));

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            if (!closestEnemy.Unit.IsFlying)
            {
                range = 10; // 10 range for ground
            }
            else if (closestEnemy.Unit.DisplayType != DisplayType.Visible)
            {
                range = 12; // sight range
            }

            var avoidPoint = GetPositionFromRange(commander, closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        public override Point2D GetPositionFromRange(UnitCommander commander, Point target, Point position, float range, float angleOffset = 0)
        {
            if (range > 10 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.IsFlying))
            {
                range = 10;
            }

            return base.GetPositionFromRange(commander, target, position, range, angleOffset);
        }
    }
}
