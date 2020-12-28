using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace Sharky.MicroControllers.Protoss
{
    public class TempestMicroController : IndividualMicroController
    {
        public TempestMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, ActiveUnitData activeUnitData, DebugManager debugManager, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, activeUnitData, debugManager, sharkyPathFinder, baseData, sharkyOptions, damageService, microPriority, groupUpEnabled)
        {
        }

        protected override bool MaintainRange(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
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
                if (DamageService.CanDamage(enemyAttack.Weapons, commander.UnitCalculation.Unit) && InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            }

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
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

            var avoidPoint = GetPositionFromRange(closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }
    }
}
