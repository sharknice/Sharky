using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class OracleMicroController : IndividualMicroController
    {
        float RevelationRange = 9;
        float RevelationRadius = 6;

        public OracleMicroController(MapDataService mapDataService, SharkyUnitData unitDataManager, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var cloakedPosition = CloakedInvader(commander);
            if (cloakedPosition != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_ORACLEREVELATION, cloakedPosition);
                return true;
            }

            var order = commander.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.EFFECT_ORACLEREVELATION && o.TargetWorldSpacePos != null);
            if (order != null && commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_ORACLEREVELATION))
            {
                if (commander.UnitCalculation.Unit.Shield > commander.UnitCalculation.Unit.ShieldMax / 2.0)
                {
                    return true;
                }

                if (Revelation(commander, frame, out action))
                {
                    return true;
                }

            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        Point2D CloakedInvader(UnitCommander commander)
        {
            var pos = commander.UnitCalculation.Position;

            var hiddenUnits = ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.DisplayType == DisplayType.Hidden).OrderBy(e => Vector2.DistanceSquared(pos, e.Value.Position));
            if (hiddenUnits.Count() > 0)
            {
                return new Point2D { X = hiddenUnits.FirstOrDefault().Value.Unit.Pos.X, Y = hiddenUnits.FirstOrDefault().Value.Unit.Pos.Y };
            }

            var unit = ActiveUnitData.SelfUnits.Values.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS).SelectMany(a => a.NearbyEnemies).Where(e => SharkyUnitData.CloakableAttackers.Contains((UnitTypes)e.Unit.UnitType) && !e.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)).OrderBy(e => Vector2.DistanceSquared(pos, e.Position)).FirstOrDefault();
            if (unit != null)
            {
                return new Point2D { X = unit.Unit.Pos.X, Y = unit.Unit.Pos.Y };
            }

            return null;
        }

        bool Revelation(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 25 || !commander.AbilityOffCooldown(Abilities.EFFECT_ORACLEREVELATION, frame, SharkyOptions.FramesPerSecond, SharkyUnitData) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON))
            {
                return false;
            }

            var cloackedPosition = CloakedInvader(commander);
            if (cloackedPosition != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_ORACLEREVELATION, cloackedPosition);
                return true;
            }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)))
            {
                return false; // TODO: unless a unit is invisible
            }

            var revelationLocation = GetBestRevelationLocation(commander);
            if (revelationLocation != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_ORACLEREVELATION, revelationLocation);
                return true;
            }

            return false;
        }

        Point2D GetBestRevelationLocation(UnitCommander commander)
        {
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < RevelationRange * RevelationRange);

            if (enemiesInRange.Count() == 0)
            {
                enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure));
                if (enemiesInRange.Count() == 0)
                {
                    return null;
                }
            }

            var hitCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                float hits = 0;
                foreach (var hitEnemy in enemiesInRange)
                {
                    if (Vector2.DistanceSquared(hitEnemy.Position, enemyAttack.Position) <= (hitEnemy.Unit.Radius + RevelationRadius) * (hitEnemy.Unit.Radius + RevelationRadius))
                    {
                        hits += 1;
                    }
                }
                hitCounts[enemyAttack.Unit.Pos] = hits;
            }

            var position = hitCounts.OrderByDescending(x => x.Value).First().Key;

            return new Point2D { X = position.X, Y = position.Y }; // TODO: if there are any cloaked units, go for the spot that hits the most cloaked units
        }

        protected override bool WeaponReady(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON);
        }

        protected override bool MaintainRange(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var range = 9f;
            var enemiesInRange = new List<UnitCalculation>();

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            }

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (PulsarBeam(commander, frame, bestTarget, out action))
            {
                return true;
            }

            if (Revelation(commander, frame, out action))
            {
                return true;
            }

            if (StasisWard(commander, frame, bestTarget, out action))
            {
                return true;
            }

            return false;
        }

        bool PulsarBeam(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (DeactivatePulsarBeam(commander, frame, bestTarget, out action))
            {
                return true;
            }

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON) || commander.UnitCalculation.Unit.Energy < 50 || bestTarget == null)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
            {
                action = commander.Order(frame, Abilities.BEHAVIOR_PULSARBEAMON);
                return true;
            }

            return false;
        }

        bool DeactivatePulsarBeam(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON))
            {
                return false;
            }

            if (bestTarget == null || !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
            {
                action = commander.Order(frame, Abilities.BEHAVIOR_PULSARBEAMOFF);
                return true;
            }

            return false;
        }

        bool StasisWard(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false; // TODO:  stasis ward, put stasis wards on the tops of ramps
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillWorkers;
            }

            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null && (bestTarget.UnitClassifications.Contains(UnitClassification.Worker) || !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker))))
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible)
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        public override List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageAir) > 0)
            {
                if (commander.RetreatPathFrame + 20 < frame)
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                    commander.RetreatPathFrame = frame;
                }

                if (FollowPath(commander, frame, out action)) { return action; }
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestHarassTarget(commander, target);

            if (PulsarBeam(commander, frame, bestTarget, out action)) { return action; }

            if (WeaponReady(commander))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            return NavigateToPoint(commander, target, defensivePoint, null, frame);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.MOVE, target);
        }
    }
}
