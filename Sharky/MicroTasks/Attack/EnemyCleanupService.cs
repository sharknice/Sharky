using SC2APIProtocol;
using Sharky.MicroControllers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Attack
{
    public class EnemyCleanupService
    {
        IMicroController MicroController;

        DamageService DamageService;

        public EnemyCleanupService(IMicroController microController, DamageService damageService)
        {
            MicroController = microController;
            DamageService = damageService;
        }

        public List<SC2APIProtocol.Action> CleanupEnemies(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            if (commanders.Count() == 0) { return null; }

            var winners = commanders.Where(u => u.UnitCalculation.TargetPriorityCalculation.Overwhelm && u.UnitCalculation.NearbyEnemies.Take(25).Count(e => DamageService.CanDamage(u.UnitCalculation, e) && e.Unit.DisplayType == DisplayType.Visible) > 0 && !u.UnitCalculation.NearbyAllies.Take(25).Any(a => !a.TargetPriorityCalculation.Overwhelm) && u.UnitCalculation.NearbyEnemies.All(e => e.FrameLastSeen == frame));
            if (winners.Count() > 0)
            {
                var defenseVector = new Vector2(defensivePoint.X, defensivePoint.Y);
                var winner = winners.OrderBy(u => Vector2.DistanceSquared(u.UnitCalculation.Position, defenseVector)).FirstOrDefault();
                var winPoint = winner.UnitCalculation.NearbyEnemies.FirstOrDefault().Position;

                return MicroController.Attack(commanders, new Point2D { X = winPoint.X, Y = winPoint.Y }, defensivePoint, armyPoint, frame);
            }

            return null;
        }
    }
}
