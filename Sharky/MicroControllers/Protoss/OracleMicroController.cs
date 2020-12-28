using SC2APIProtocol;
using Sharky.Managers;
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

        public OracleMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, ActiveUnitData activeUnitData, DebugManager debugManager, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, activeUnitData, debugManager, sharkyPathFinder, baseData, sharkyOptions, damageService, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
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
            var pos = new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);

            var hiddenUnits = ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.DisplayType == DisplayType.Hidden).OrderBy(e => Vector2.DistanceSquared(pos, new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y)));
            if (hiddenUnits.Count() > 0)
            {
                return new Point2D { X = hiddenUnits.FirstOrDefault().Value.Unit.Pos.X, Y = hiddenUnits.FirstOrDefault().Value.Unit.Pos.Y };
            }

            var unit = ActiveUnitData.SelfUnits.Values.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS).SelectMany(a => a.NearbyEnemies).Where(e => UnitDataManager.CloakableAttackers.Contains((UnitTypes)e.Unit.UnitType) && !e.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)).OrderBy(e => Vector2.DistanceSquared(pos, new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y))).FirstOrDefault();
            if (unit != null)
            {
                return new Point2D { X = unit.Unit.Pos.X, Y = unit.Unit.Pos.Y };
            }

            return null;
        }

        bool Revelation(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 25 || !commander.AbilityOffCooldown(Abilities.EFFECT_ORACLEREVELATION, frame, SharkyOptions.FramesPerSecond, UnitDataManager) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON))
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
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < RevelationRange * RevelationRange);

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
                    if (Vector2.DistanceSquared(new Vector2(hitEnemy.Unit.Pos.X, hitEnemy.Unit.Pos.Y), new Vector2(enemyAttack.Unit.Pos.X, enemyAttack.Unit.Pos.Y)) <= (hitEnemy.Unit.Radius + RevelationRadius) * (hitEnemy.Unit.Radius + RevelationRadius))
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

        protected override bool MaintainRange(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            var range = 9f;
            var enemiesInRange = new List<UnitCalculation>();

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack.Weapons, commander.UnitCalculation.Unit) && InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            }

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
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

        bool PulsarBeam(UnitCommander commander, int frame, UnitCalculation bestTarget, out SC2APIProtocol.Action action)
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

        bool DeactivatePulsarBeam(UnitCommander commander, int frame, UnitCalculation bestTarget, out SC2APIProtocol.Action action)
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

        bool StasisWard(UnitCommander commander, int frame, UnitCalculation bestTarget, out SC2APIProtocol.Action action)
        {
            action = null;
            return false; // TODO:  stasis ward, put stasis wards on the tops of ramps
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillWorkers;
            }

            return base.AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action);
        }

        protected override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
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

        public SC2APIProtocol.Action NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            SC2APIProtocol.Action action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageAir) > 0)
            {
                if (commander.RetreatPathFrame + 20 < frame)
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(target.X, target.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, frame);
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

        public SC2APIProtocol.Action HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            SC2APIProtocol.Action action = null;

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

        protected UnitCalculation GetBestHarassTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && u.UnitClassifications.Contains(UnitClassification.Worker))); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapon, a.Unit, a.UnitTypeData));
                if (oneShotKills.Count() > 0)
                {
                    if (existingAttackOrder != null)
                    {
                        var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }

                    var oneShotKill = GetBestTargetFromList(commander, oneShotKills, existingAttackOrder);
                    if (oneShotKill != null)
                    {
                        return oneShotKill;
                    }
                    else
                    {
                        commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();
                        return commander.BestTarget;
                    }
                }

                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null && bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = new List<UnitCalculation>(); // nearby units not in range right now
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (enemyAttack.Unit.DisplayType != DisplayType.Hidden && enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && !InRange(enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
                {
                    attacks.Add(enemyAttack);
                }
            }
            if (attacks.Count > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }
    }
}
