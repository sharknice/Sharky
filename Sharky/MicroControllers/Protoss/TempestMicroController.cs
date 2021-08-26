using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace Sharky.MicroControllers.Protoss
{
    public class TempestMicroController : IndividualMicroController
    {
        public TempestMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool MaintainRange(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive)
            {
                return false;
            }

            var range = 14; // 14 range for air
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

        protected override Point2D GetPositionFromRange(UnitCommander commander, Point target, Point position, float range)
        {
            if (range > 10 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.IsFlying))
            {
                range = 10;
            }

            var angle = Math.Atan2(target.Y - position.Y, position.X - target.X);
            var x = range * Math.Cos(angle);
            var y = range * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }
    }
}
