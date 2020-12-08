using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class MothershipMicroController : IndividualMicroController
    {
        int CloakRange = 5;
        int TimeWarpRange = 9;
        float TImeWarpRadius = 3.5f;

        public MothershipMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, IBaseManager baseManager, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, baseManager, sharkyOptions, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (TimeWarp(commander, frame, out action))
            {
                return true;
            }

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool TimeWarp(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 100 || !commander.AbilityOffCooldown(Abilities.EFFECT_TIMEWARP, frame, SharkyOptions.FramesPerSecond, UnitDataManager))
            {
                return false;
            }

            var point = GetTimeWarpLocation(commander);

            if (point == null)
            {
                return false;
            }

            action = commander.Order(frame, Abilities.EFFECT_TIMEWARP, point);
            return true;
        }

        Point2D GetTimeWarpLocation(UnitCommander commander)
        {
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < TimeWarpRange * TimeWarpRange);

            var damageCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                float damageReduction = 0;
                foreach (var hitEnemy in enemiesInRange)
                {
                    if (!hitEnemy.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(new Vector2(hitEnemy.Unit.Pos.X, hitEnemy.Unit.Pos.Y), new Vector2(enemyAttack.Unit.Pos.X, enemyAttack.Unit.Pos.Y)) <= (hitEnemy.Unit.Radius + TImeWarpRadius) * (hitEnemy.Unit.Radius + TImeWarpRadius))
                    {
                        damageReduction += hitEnemy.Dps;
                    }
                }
                damageCounts[enemyAttack.Unit.Pos] = damageReduction;
            }

            return GetBestTimeWarpLocation(damageCounts.OrderByDescending(x => x.Value));
        }

        Point2D GetBestTimeWarpLocation(IOrderedEnumerable<KeyValuePair<Point, float>> locations)
        {
            foreach (var location in locations)
            {
                if (location.Value < 150)
                {
                    return null;
                }

                var placement = new Point2D { X = location.Key.X, Y = location.Key.Y };
                bool good = true;
                if (!MapDataService.SelfVisible(placement))
                {
                    continue;
                }

                if (good)
                {
                    return placement;
                }
            }
            return null;
        }

        bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax / 2)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (commander.UnitCalculation.Unit.Shield < 1)
                {
                    if (Retreat(commander, target, defensivePoint, frame, out action))
                    {
                        return true;
                    }
                }
            }


            // follow behind at the range of cloak field
            var unitToSupport = GetSupportTarget(commander, target, defensivePoint);

            if (unitToSupport == null)
            {
                return false;
            }

            var moveTo = GetSupportSpot(new Point2D { X = unitToSupport.Unit.Pos.X, Y = unitToSupport.Unit.Pos.Y }, defensivePoint);

            action = commander.Order(frame, Abilities.MOVE, moveTo);
            return true;
        }

        UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint)
        {
            var allyAttacks = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit));

            // out of nearby allies within 15 range
            // select the friendlies with enemies in 15 range
            // order by closest to the enemy
            var friendlies = allyAttacks.Where(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < 225
                    && u.Value.NearbyEnemies.Any(e => DistanceSquared(u.Value, e) < 225)
                ).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).First(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            // if none
            // get any allies
            // select the friendies with enemies in 15 range
            // order by closest to the enemy
            friendlies = allyAttacks.Where(u => u.Value.NearbyEnemies.Any(e => DistanceSquared(u.Value, e) < 225)).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).First(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            // if still none
            //get ally closest to target
            friendlies = allyAttacks.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(target.X, target.Y)));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            return null;
        }

        float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            return Vector2.DistanceSquared(new Vector2(unit1.Unit.Pos.X, unit1.Unit.Pos.Y), new Vector2(unit2.Unit.Pos.X, unit2.Unit.Pos.Y));
        }

        Point2D GetSupportSpot(Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(target.Y - defensivePoint.Y, defensivePoint.X - target.X);
            var x = CloakRange * Math.Cos(angle);
            var y = CloakRange * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }
    }
}
