using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Sharky.MicroControllers
{
    public class IndividualMicroController : IIndividualMicroController
    {
        protected MapDataService MapDataService;
        protected UnitDataManager UnitDataManager;
        protected UnitManager UnitManager;
        protected DebugManager DebugManager;
        protected IPathFinder SharkyPathFinder;
        protected SharkyOptions SharkyOptions;
        protected MicroPriority MicroPriority;

        public bool GroupUpEnabled;
        protected float GroupUpDistanceSmall;
        protected float GroupUpDistance;
        protected float GroupUpDistanceMax;
        protected float AvoidDamageDistance;
        protected float LooseFormationDistance;

        public IndividualMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, UnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled)
        {
            MapDataService = mapDataService;
            UnitDataManager = unitDataManager;
            UnitManager = unitManager;
            DebugManager = debugManager;
            SharkyPathFinder = sharkyPathFinder;
            SharkyOptions = sharkyOptions;
            MicroPriority = microPriority;
            GroupUpEnabled = groupUpEnabled;

            GroupUpDistanceSmall = 5;
            GroupUpDistance = 10;
            GroupUpDistanceMax = 50;
            AvoidDamageDistance = 1;
            LooseFormationDistance = 1.75f;
        }

        public virtual SC2APIProtocol.Action Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.IsSelected)
            {
                var breakpoint = true;
            }

            SC2APIProtocol.Action action = null;

            // TODO: all the SHarkMicroController.cs stuff
            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target);

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, out action)) { return action; }

            if (WeaponReady(commander))
            {
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual SC2APIProtocol.Action Idle(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            return null;
        }

        public virtual SC2APIProtocol.Action Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(defensivePoint.X, defensivePoint.Y)) > 100)
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out SC2APIProtocol.Action action)) { return action; }
                return commander.Order(frame, Abilities.MOVE, defensivePoint);
            }
            return null;
        }

        protected bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out SC2APIProtocol.Action action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (AvoidPurificationNovas(commander, target, defensivePoint, frame, out action)) { return true; }

            // TODO: special case movement
            //if (ChargeBlindly(commander, target))
            //{
            //    return true;
            //}

            //if (ChargeUpRamp(commander, target, bestTarget))
            //{
            //    return true;
            //}

            //if (DealWithSiegedTanks(commander))
            //{
            //    return true;
            //}

            //// TODO: DealWithCyclones(agent)
            //// if overwhelming victory charge the cyclone and kill it, otherwise stay out of lockon range
            //if (DealWithCyclones(commander, defensivePoint))
            //{
            //    return true;
            //}

            //if (FollowShades(commander))
            //{
            //    return true;
            //}

            if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        protected virtual bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (GetInFormation(commander, formation, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(enemy => enemy.Unit.Tag == bestTarget.Unit.Tag))
            {
                if ((MicroPriority == MicroPriority.NavigateToLocation) && !commander.UnitCalculation.EnemiesInRange.Any(enemy => enemy.Unit.Tag == bestTarget.Unit.Tag))
                {

                }
                else
                {
                    return MoveToAttackTarget(commander, bestTarget, frame, out action);
                }
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation && MapDataService.SelfVisible(bestTarget.Unit.Pos))
            {
                return MoveToAttackTarget(commander, bestTarget, frame, out action);
            }

            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        protected virtual bool MoveToAttackTarget(UnitCommander commander, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(bestTarget.Unit.Pos))
            {
                var attackPoint = GetPositionFromRange(bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range + bestTarget.Unit.Radius + commander.UnitCalculation.Unit.Radius);
                action = commander.Order(frame, Abilities.MOVE, attackPoint);
                return true;
            }
            else
            {
                action = commander.Order(frame, Abilities.MOVE, new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y });
                return true;
            }
        }

        protected virtual bool GetInFormation(UnitCommander commander, Formation formation, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (formation == Formation.Normal)
            {
                return false;
            }

            if (formation == Formation.Loose)
            {
                var closestAlly = commander.UnitCalculation.NearbyAllies.OrderBy(a => Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y))).FirstOrDefault();
                if (closestAlly != null)
                {
                    if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(closestAlly.Unit.Pos.X, closestAlly.Unit.Pos.Y)) < (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius) * (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius))
                    {
                        var avoidPoint = GetPositionFromRange(closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius + .5f);
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            if (formation == Formation.Tight)
            {
                var vectors = commander.UnitCalculation.NearbyAllies.Where(a => (!a.Unit.IsFlying && !commander.UnitCalculation.Unit.IsFlying) || (a.Unit.IsFlying && commander.UnitCalculation.Unit.IsFlying)).Select(u => new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y));
                if (vectors.Count() > 0)
                {
                    var center = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
                    if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(center.X, center.Y)) + commander.UnitCalculation.Unit.Radius > 1)
                    {
                        action = commander.Order(frame, Abilities.MOVE, center);
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool MoveAway(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.TargetPriorityCalculation.Overwhelm || MicroPriority == MicroPriority.AttackForward)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action)) { return true; }


                if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25 && AvoidDamage(commander, target, defensivePoint, frame, out action)) // TODO: this only works for protoss, if we want it to work for zerg and terran it needs to change
                {
                    return true;
                }
            }

            if (MicroPriority == MicroPriority.LiveAndAttack)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = new ConcurrentBag<UnitCalculation>();

            Parallel.ForEach(commander.UnitCalculation.NearbyEnemies, (enemyAttack) =>
            {
                if (UnitManager.CanDamage(enemyAttack.Weapons, commander.UnitCalculation.Unit) && InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, UnitDataManager.GetRange(enemyAttack.Unit) + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    attacks.Add(enemyAttack);
                }
            });

            if (attacks.Count > 0)
            {
                var attack = attacks.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y))).FirstOrDefault();  // enemies that are closest to being outranged
                var range = UnitDataManager.GetRange(attack.Unit);
                if (attack.Range > range)
                {
                    range = attack.Range;
                }

                // TODO: real pathing
                //IEnumerable<Vector2> path;
                //if (commander.UnitCalculation.Unit.IsFlying)
                //{
                //    path = SharkPathFinder.GetSafeAirPath(defensivePoint.X, defensivePoint.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);
                //}
                //else
                //{
                //    path = SharkPathFinder.GetSafeGroundPath(defensivePoint.X, defensivePoint.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);
                //}
                //if (FollowPath(commander, path))
                //{
                //    return true;
                //}

                var avoidPoint = GetGroundAvoidPoint(commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            if (MaintainRange(commander, frame, out action)) { return true; }

            return false;
        }

        protected virtual bool MaintainRange(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive)
            {
                return false;
            }

            var range = commander.UnitCalculation.Range;
            var enemiesInRange = new ConcurrentBag<UnitCalculation>();

            Parallel.ForEach(commander.UnitCalculation.NearbyEnemies, (enemyAttack) =>
            {
                if (UnitManager.CanDamage(enemyAttack.Weapons, commander.UnitCalculation.Unit) && InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            });

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        protected bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            
            // if you can outrun and outrange the enemy do that isntead of a full on retreat
            var closestEnemy = commander.UnitCalculation.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();

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

            if (closestEnemy != null && commander.RetreatPathFrame + 5 < frame)
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(defensivePoint.X, defensivePoint.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, frame);
                }
                else
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(defensivePoint.X, defensivePoint.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, frame);
                }
                commander.RetreatPathFrame = frame;
            }

            if (FollowPath(commander, commander.RetreatPath, frame, out action)) { return true; }

            action = commander.Order(frame, Abilities.MOVE, defensivePoint);
            return true;
        }

        protected bool FollowPath(UnitCommander commander, IEnumerable<Vector2> path, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (path.Count() > 1)
            {
                if (SharkyOptions.Debug)
                {
                    var thing = path.ToList();

                    DebugManager.DrawSphere(new Point { X = thing[0].X, Y = thing[0].Y, Z = commander.UnitCalculation.Unit.Pos.Z }, 1, new Color { R = 0, G = 0, B = 255 });
                    for (int index = 0; index < thing.Count - 1; index++)
                    {
                        DebugManager.DrawLine(new Point { X = thing[index].X, Y = thing[index].Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1 }, new Point { X = thing[index + 1].X, Y = thing[index + 1].Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1 }, new Color { R = 0, G = 0, B = 255 });
                    }
                }

                var position = new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);
                foreach (var point in path.Skip(1))
                {
                    if (Vector2.DistanceSquared(position, point) > 1)
                    {
                        action = commander.Order(frame, Abilities.MOVE, new Point2D { X = point.X, Y = point.Y });
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual UnitCalculation GetBestTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new ConcurrentBag<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapon, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
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
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
                //if (bestAttack != null && MapAnalyzer.IsChoke(bestAttack.Unit.Pos)) // TODO: if it's a blocking a choke point attack it
                //{
                //    commander.BestTarget = bestAttack;
                //    return bestAttack;
                //}
            }

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST)
            {
                range = 10;
            }
            // TODO: don't go attack units super far away if there are still units that can't attack this unit, but are close
            attacks = new ConcurrentBag<UnitCalculation>(); // nearby units not in range right now
            Parallel.ForEach(commander.UnitCalculation.NearbyEnemies, (enemyAttack) =>
            {
                if (enemyAttack.Unit.DisplayType != DisplayType.Hidden && UnitManager.CanDamage(commander.UnitCalculation.Weapons, enemyAttack.Unit) && !InRange(enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
                {
                    attacks.Add(enemyAttack);
                }
            });
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

            if (MapDataService.SelfVisible(target)) // if enemy main is unexplored, march to enemy main
            {
                var fakeMainBase = new Unit(commander.UnitCalculation.Unit);
                fakeMainBase.Pos = new Point { X = target.X, Y = target.Y, Z = 1 };
                return new UnitCalculation(fakeMainBase, fakeMainBase, 0, UnitDataManager, SharkyOptions);
            }
            var unitsNearEnemyMain = UnitManager.EnemyUnits.Values.Where(e => InRange(target, e.Unit.Pos, 20));
            if (unitsNearEnemyMain.Count() > 0 && InRange(target, commander.UnitCalculation.Unit.Pos, 100))
            {
                attacks = new ConcurrentBag<UnitCalculation>(); // enemies in the main enemy base
                Parallel.ForEach(unitsNearEnemyMain, (enemyAttack) =>
                {
                    if (enemyAttack.Unit.DisplayType != DisplayType.Hidden && UnitManager.CanDamage(commander.UnitCalculation.Weapons, enemyAttack.Unit))
                    {
                        attacks.Add(enemyAttack);
                    }
                });
                if (attacks.Count > 0)
                {
                    var bestMainAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                    if (bestMainAttack != null && (bestMainAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestMainAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                    {
                        commander.BestTarget = bestMainAttack;
                        return bestMainAttack;
                    }
                    if (bestAttack == null)
                    {
                        bestAttack = bestMainAttack;
                    }
                }
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        protected virtual bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                action = commander.Order(frame, Abilities.ATTACK, defensivePoint); // no damaging targets in range, retreat towards the defense point
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.ATTACK, new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y });
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (UnitDataManager.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit) || a.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    return false;
                }
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.ATTACK, new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y });
                return true;
            }

            action = commander.Order(frame, Abilities.ATTACK, target); // no damaging targets in range, attack towards the main target
            return true;
        }

        protected virtual bool GroupUp(UnitCommander commander, Point2D target, Point2D groupCenter, bool attack, int frame, out SC2APIProtocol.Action action)
        {
            action = null;
            if (commander.UnitCalculation.NearbyEnemies.Any())
            {
                return false;
            }

            if (frame > SharkyOptions.FramesPerSecond * 10 * 60 && UnitManager.EnemyUnits.Count() < 10)
            {
                return false; // stop grouping up when searching for the last enemy units to finish the game
            }

            // if not near the center of all the units attacking
            // move toward the center
            var groupUpSmall = false;
            if (commander.UnitCalculation.NearbyAllies.Count < 10 && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) > GroupUpDistanceSmall * GroupUpDistanceSmall && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < GroupUpDistanceMax * GroupUpDistanceMax)
            {
                groupUpSmall = true;
            }
            if (groupUpSmall || (Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) > GroupUpDistance * GroupUpDistance && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < GroupUpDistanceMax * GroupUpDistanceMax))
            {
                if (!commander.UnitCalculation.Unit.IsFlying && !MapDataService.PathWalkable(groupCenter))
                {
                    return false;
                }

                if (attack)
                {
                    DebugManager.DrawSphere(new Point { X = groupCenter.X, Y = groupCenter.Y, Z = 10 });
                    action = commander.Order(frame, Abilities.ATTACK, groupCenter);
                }
                else
                {
                    action = commander.Order(frame, Abilities.MOVE, groupCenter);
                }

                return true;
            }

            return false;
        }

        protected virtual bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible)
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        protected virtual UnitCalculation GetBestTargetFromList(UnitCommander commander, IEnumerable<UnitCalculation> attacks, UnitOrder existingAttackOrder)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillDetection)
            {
                var detectingEnemies = attacks.Where(u => UnitDataManager.DetectionTypes.Contains((UnitTypes)u.Unit.UnitType)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
                if (existingAttackOrder != null)
                {
                    var existing = detectingEnemies.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = detectingEnemies.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var enemy = detectingEnemies.FirstOrDefault();
                if (enemy != null)
                {
                    return enemy;
                }

                var abilityDetectingEnemies = attacks.Where(u => UnitDataManager.AbilityDetectionTypes.Contains((UnitTypes)u.Unit.UnitType)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
                if (existingAttackOrder != null)
                {
                    var existing = abilityDetectingEnemies.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = abilityDetectingEnemies.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                enemy = abilityDetectingEnemies.FirstOrDefault();
                if (enemy != null)
                {
                    return enemy;
                }
            }

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillWorkers)
            {
                var scvs = attacks.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
                if (existingAttackOrder != null)
                {
                    var existing = scvs.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = scvs.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var scv = scvs.FirstOrDefault();
                if (scv != null)
                {
                    return scv;
                }
            }
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillBunker)
            {
                var bunkers = attacks.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));

                if (existingAttackOrder != null)
                {
                    var existing = bunkers.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = bunkers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var bunker = bunkers.FirstOrDefault();
                if (bunker != null)
                {
                    return bunker;
                }
            }

            var weapon = UnitDataManager.GetWeapon(commander.UnitCalculation.Unit);

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
            {
                var airAttackers = attacks.Where(u => u.DamageAir && AirAttackersFilter(commander, u));

                if (airAttackers.Count() > 0)
                {
                    var bestDpsReduction = GetBestDpsReduction(commander, weapon, airAttackers, attacks);

                    if (existingAttackOrder != null)
                    {
                        var existingReduction = airAttackers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                        if (existingReduction == null && commander.BestTarget != null)
                        {
                            existingReduction = airAttackers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        }
                        if (existingReduction != null)
                        {
                            var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                            var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                            if (existing * 1.25 > best)
                            {
                                return existingReduction; // just keep attacking the same unit
                            }
                        }
                    }

                    if (bestDpsReduction != null)
                    {
                        return bestDpsReduction;
                    }
                }
            }
            else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
            {
                var groundAttackers = attacks.Where(u => u.DamageGround && u.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && (!u.UnitClassifications.Contains(UnitClassification.Worker) || u.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag)) && GroundAttackersFilter(commander, u));
                if (groundAttackers.Count() > 0)
                {
                    var bestDpsReduction = GetBestDpsReduction(commander, weapon, groundAttackers, attacks);

                    if (existingAttackOrder != null)
                    {
                        var existingReduction = groundAttackers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                        if (existingReduction == null && commander.BestTarget != null)
                        {
                            existingReduction = groundAttackers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        }
                        if (existingReduction != null)
                        {
                            var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                            var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                            if (existing * 1.25 > best)
                            {
                                return existingReduction; // just keep attacking the same unit
                            }
                        }
                    }

                    if (bestDpsReduction != null)
                    {
                        return bestDpsReduction;
                    }
                }
            }

            var threats = attacks.Where(enemyAttack => enemyAttack.Damage > 0 && UnitManager.CanDamage(enemyAttack.Weapons, commander.UnitCalculation.Unit) && enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && (!enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) || enemyAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag)) && GroundAttackersFilter(commander, enemyAttack) && AirAttackersFilter(commander, enemyAttack));
            if (threats.Count() > 0)
            {
                var bestDpsReduction = GetBestDpsReduction(commander, weapon, threats, attacks);
                if (existingAttackOrder != null)
                {
                    var existingReduction = threats.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                    if (commander.BestTarget != null)
                    {
                        var existing = threats.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            existingReduction = existing;
                        }
                    }
                    if (existingReduction != null)
                    {
                        var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                        var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                        if (existing * 1.25 > best)
                        {
                            return existingReduction; // just keep attacking the same unit
                        }
                    }
                }
                if (bestDpsReduction != null)
                {
                    return bestDpsReduction;
                }
            }

            var defensiveBuildings = attacks.Where(enemyAttack => enemyAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && GroundAttackersFilter(commander, enemyAttack)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
            var defensiveBuilding = defensiveBuildings.FirstOrDefault();
            if (commander.BestTarget != null)
            {
                var existing = defensiveBuildings.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                if (existing != null)
                {
                    defensiveBuilding = existing;
                }
            }
            if (defensiveBuilding != null)
            {
                return defensiveBuilding;
            }

            var workers = attacks.Where(enemyAttack => enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && GroundAttackersFilter(commander, enemyAttack)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
            if (workers.Count() > 0)
            {
                if (existingAttackOrder != null)
                {
                    var existingReduction = workers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                    if (commander.BestTarget != null)
                    {
                        var existing = workers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            existingReduction = existing;
                        }
                    }
                    if (existingReduction != null)
                    {
                        return existingReduction; // just keep attacking the same unit
                    }
                }
                return workers.FirstOrDefault();
            }

            var building = GetBestBuildingTarget(attacks, commander);
            if (building != null)
            {
                return building;
            }

            return null;
        }

        protected UnitCalculation GetBestBuildingTarget(IEnumerable<UnitCalculation> attacks, UnitCommander commander)
        {
            var orderedAttacks = attacks.Where(enemy => enemy.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && enemy.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && enemy.Unit.UnitType != (uint)UnitTypes.ZERG_EGG && GroundAttackersFilter(commander, enemy)).OrderBy(enemy => (UnitTypes)enemy.Unit.UnitType, new UnitTypeTargetPriority()).ThenBy(enemy => enemy.Unit.Health + enemy.Unit.Shield).ThenBy(enemy => Vector2.DistanceSquared(new Vector2(enemy.Unit.Pos.X, enemy.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)));
            var activeBuilding = orderedAttacks.Where(a => a.Unit.IsActive).FirstOrDefault();
            if (activeBuilding != null && !activeBuilding.UnitClassifications.Contains(UnitClassification.ResourceCenter))
            {
                return activeBuilding;
            }
            else
            {
                if (commander.BestTarget != null)
                {
                    var existing = orderedAttacks.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                    if (existing != null)
                    {
                        return existing; // just keep attacking the same unit
                    }
                }

                return orderedAttacks.FirstOrDefault();
            }
        }

        protected virtual UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            var bestDpsReduction = primaryTargets.OrderByDescending(enemy => enemy.Dps / TimeToKill(weapon, enemy.Unit, enemy.UnitTypeData)).ThenBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();

            return bestDpsReduction;
        }

        protected float TimeToKill(Weapon weapon, Unit unit, UnitTypeData unitTypeData)
        {
            float bonus = 0;
            if (unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD))
            {
                bonus += 100;
            }
            var damage = GetDamage(weapon, unit, unitTypeData);
            return (unit.Health + unit.Shield + bonus) / (damage / weapon.Speed);
        }

        protected virtual float GetDamage(Weapon weapon, Unit unit, UnitTypeData unitTypeData)
        {
            if (weapon == null || weapon.Damage == 0)
            {
                return 0;
            }
            if ((unit.IsFlying || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)) && weapon.Type == Weapon.Types.TargetType.Ground)
            {
                return 0;
            }
            if (!unit.IsFlying && weapon.Type == Weapon.Types.TargetType.Air && unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                return 0;
            }

            float bonusDamage = 0;
            var damageBonus = weapon.DamageBonus.FirstOrDefault(d => unitTypeData.Attributes.Contains(d.Attribute));
            if (damageBonus != null)
            {
                bonusDamage += damageBonus.Bonus;
            }

            float bonusArmor = 0;
            if (unit.BuffIds.Contains(76))
            {
                bonusArmor += 2;
            }

            var damage = (weapon.Damage + bonusDamage) - (unitTypeData.Armor + bonusArmor); // TODO: weapon and armor upgrades

            if (damage < 0.5)
            {
                damage = 0.5f;
            }

            return damage * weapon.Attacks;
        }

        protected virtual bool WeaponReady(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown == 0;
        }

        protected virtual bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, out SC2APIProtocol.Action action)
        {
            action = null;
            return false;
        }

        protected virtual Formation GetDesiredFormation(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.IsFlying)
            {
                if (MapDataService.GetCells(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                {
                    return Formation.Loose;
                }
                else
                {
                    return Formation.Normal;
                }
            }

            var zerglingDps = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED).Sum(e => e.Dps);
            var splashDps = commander.UnitCalculation.NearbyEnemies.Where(e => UnitDataManager.GroundSplashDamagers.Contains((UnitTypes)e.Unit.UnitType)).Sum(e => e.Dps);

            if (zerglingDps > splashDps)
            {
                return Formation.Tight;
            }
            if (splashDps > 0)
            {
                return Formation.Loose;
            }

            return Formation.Normal;
        }

        protected virtual bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;
            var attack = commander.UnitCalculation.Attackers.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y))).FirstOrDefault();
            if (attack != null)
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    var avoidPoint = GetAirAvoidPoint(commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
                else
                {
                    var avoidPoint = GetGroundAvoidPoint(commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidPurificationNovas(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.IsFlying) { return false; }

            var nova = commander.UnitCalculation.NearbyEnemies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < 25).OrderBy(a => Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
            if (nova == null)
            {
                nova = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && a.Unit.BuffDurationRemain < a.Unit.BuffDurationMax / 2 && Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < 25).OrderBy(a => Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
            }

            if (nova != null)
            {
                var avoidPoint = GetGroundAvoidPoint(commander.UnitCalculation.Unit.Pos, nova.Unit.Pos, target, defensivePoint, 5);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            return false;
        }

        protected virtual Point2D GetPositionFromRange(Point target, Point position, float range)
        {
            var angle = Math.Atan2(target.Y - position.Y, position.X - target.X);
            var x = range * Math.Cos(angle);
            var y = range * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }

        protected virtual Point2D GetGroundAvoidPoint(Point start, Point avoid, Point2D target, Point2D defensivePoint, float range)
        {
            var avoidPoint = GetPositionFromRange(avoid, start, range);
            if (!MapDataService.PathWalkable(start, avoidPoint))
            {
                if (Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(target.X, target.Y)) < Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(defensivePoint.X, defensivePoint.Y)))
                {
                    avoidPoint = target;
                }
                else
                {
                    avoidPoint = defensivePoint;
                }
            }
            return avoidPoint;
        }

        protected virtual Point2D GetAirAvoidPoint(Point start, Point avoid, Point2D target, Point2D defensivePoint, float range)
        {
            var avoidPoint = GetPositionFromRange(avoid, start, range);
            if (!MapDataService.PathFlyable(start, avoidPoint))
            {
                if (Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(target.X, target.Y)) < Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(defensivePoint.X, defensivePoint.Y)))
                {
                    avoidPoint = target;
                }
                else
                {
                    avoidPoint = defensivePoint;
                }
            }
            return avoidPoint;
        }

        protected virtual bool GroundAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            var count = enemyAttack.Attackers.Count(c => c.Unit.UnitType == commander.UnitCalculation.Unit.UnitType);

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.TargetUnitTag == enemyAttack.Unit.Tag) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == enemyAttack.Unit.Tag))
            {
                return true;
            }

            return count * commander.UnitCalculation.Damage > enemyAttack.Unit.Health + enemyAttack.Unit.Shield + enemyAttack.SimulatedHealPerSecond;
        }

        protected virtual bool AirAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            var count = enemyAttack.Attackers.Count(c => c.Unit.UnitType == commander.UnitCalculation.Unit.UnitType);

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.TargetUnitTag == enemyAttack.Unit.Tag) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == enemyAttack.Unit.Tag))
            {
                return true;
            }

            return count * commander.UnitCalculation.Damage > enemyAttack.Unit.Health + enemyAttack.Unit.Shield + enemyAttack.SimulatedHealPerSecond;
        }

        protected bool InRange(Point targetLocation, Point unitLocation, float range)
        {
            return Vector2.DistanceSquared(new Vector2(targetLocation.X, targetLocation.Y), new Vector2(unitLocation.X, unitLocation.Y)) <= (range * range);
        }

        protected bool InRange(Point2D targetLocation, Point unitLocation, float range)
        {
            return Vector2.DistanceSquared(new Vector2(targetLocation.X, targetLocation.Y), new Vector2(unitLocation.X, unitLocation.Y)) <= (range * range);
        }
    }
}
