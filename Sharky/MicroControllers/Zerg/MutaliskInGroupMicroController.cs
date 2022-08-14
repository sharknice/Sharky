using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class MutaliskInGroupMicroController : IndividualMicroController
    {
        public MutaliskInGroupMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (commander.UnitCalculation.EnemiesThreateningDamage.Count() > 0 && !commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }

            if (IgnoreDistractions && IsDistraction(commander, target, bestTarget, frame))
            {
                return false;
            }

            if (TargetEnemyMainFirst && TargetingData.AttackPoint.X == TargetingData.EnemyMainBasePoint.X && TargetingData.AttackPoint.Y == TargetingData.EnemyMainBasePoint.Y)
            {
                if (Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), commander.UnitCalculation.Position) > 100)
                {
                    if (bestTarget == null) { return false; }
                    if (!bestTarget.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !bestTarget.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && !bestTarget.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        return false;
                    }
                }
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && MicroPriority != MicroPriority.NavigateToLocation)
            {
                if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return true; }

                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }

                if (WeaponReady(commander, frame) && bestTarget.FrameLastSeen == frame)
                {
                    action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                }
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType) || commander.UnitCalculation.NearbyEnemies.Any(e => AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType)))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }
                if (WeaponReady(commander, frame))
                {
                    if (bestTarget.Unit.Tag == commander.UnitCalculation.Unit.Tag)
                    {
                        action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                    }
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                }
                return true;
            }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return true; }

            // no damaging targets in range, attack towards the main target
            action = commander.Order(frame, Abilities.MOVE, target);

            return true;
        }

        protected override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (AvoidAllDamage(commander, target, defensivePoint, frame, out action)) { return true; }

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority != TargetPriority.FullRetreat && commander.UnitCalculation.EnemiesInRange.Any() && WeaponReady(commander, frame) && !SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType)) // keep shooting as you retreat
            {
                var bestTarget = GetBestTarget(commander, target, frame);
                if (bestTarget != null && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            if (GetInBunker(commander, frame, out action)) { return true; }

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.Take(25).Where(e => DamageService.CanDamage(e, commander.UnitCalculation)).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                closestEnemy = commander.UnitCalculation.NearbyEnemies.Take(25).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            }


            if (closestEnemy != null)
            {
                if (DefendBehindWall(commander, defensivePoint, defensivePoint, frame, out action)) { return true; }

                if (commander.UnitCalculation.NearbyEnemies.Take(25).All(e => e.Range < commander.UnitCalculation.Range && e.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed))
                {
                    if (MapDataService.SelfVisible(closestEnemy.Unit.Pos))
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
                }

                if (commander.RetreatPathFrame + 20 < frame || commander.RetreatPathIndex >= commander.RetreatPath.Count())
                {
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    else
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    commander.RetreatPathFrame = frame;
                    commander.RetreatPathIndex = 1;
                }

                if (FollowPath(commander, frame, out action)) { return true; }
            }

            action = commander.Order(frame, Abilities.MOVE, defensivePoint);
            return true;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 2 || commander.UnitCalculation.Unit.WeaponCooldown > 20;
        }

        protected override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool IsDistraction(UnitCommander commander, Point2D target, UnitCalculation enemy, int frame)
        {
            if (enemy == null) { return false; }
            if (enemy.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed) 
            { 
                return false; 
            }
            return base.IsDistraction(commander, target, enemy, frame);
        }
    }
}
