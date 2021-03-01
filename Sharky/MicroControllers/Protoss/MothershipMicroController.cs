using SC2APIProtocol;
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

        public MothershipMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
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

        bool TimeWarp(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_TIMEWARP))
            {
                return true;
            }

            if (commander.UnitCalculation.Unit.Energy < 100 || !commander.AbilityOffCooldown(Abilities.EFFECT_TIMEWARP, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
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
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < TimeWarpRange * TimeWarpRange);

            var damageCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                float damageReduction = 0;
                foreach (var hitEnemy in enemiesInRange)
                {
                    if (!hitEnemy.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(hitEnemy.Position, enemyAttack.Position) <= (hitEnemy.Unit.Radius + TImeWarpRadius) * (hitEnemy.Unit.Radius + TImeWarpRadius))
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

        bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out List<SC2APIProtocol.Action> action)
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
            var armyUnits = ActiveUnitData.Commanders.Where(u => u.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit)).Select(s => s.Value);

            var unitToSupport = GetSupportTarget(commander, armyUnits, target, defensivePoint);

            if (unitToSupport == null)
            {
                return false;
            }

            var moveTo = GetSupportSpot(unitToSupport, target, defensivePoint);

            action = commander.Order(frame, Abilities.MOVE, moveTo);
            return true;
        }

        float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            return Vector2.DistanceSquared(unit1.Position, unit2.Position);
        }

        protected override Point2D GetSupportSpot(UnitCommander unitToSupport, Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(unitToSupport.UnitCalculation.Position.Y - defensivePoint.Y, defensivePoint.X - unitToSupport.UnitCalculation.Position.X);
            var x = CloakRange * Math.Cos(angle);
            var y = CloakRange * Math.Sin(angle);
            return new Point2D { X = unitToSupport.UnitCalculation.Position.X + (float)x, Y = unitToSupport.UnitCalculation.Position.Y - (float)y };
        }
    }
}
