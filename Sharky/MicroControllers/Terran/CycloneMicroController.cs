using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class CycloneMicroController : IndividualMicroController
    {
        int LastLockOnFrame;

        public CycloneMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 2f; // TODO: may need to turn off autolock on
            LastLockOnFrame = -1;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AutoCastOff)
            {
                action = commander.ToggleAutoCast(Abilities.EFFECT_LOCKON);
                commander.AutoCastOff = true;
                return true;
            }

            if (commander.CommanderState == CommanderState.MaintainLockon)
            {
                var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == commander.LastLockOn.Tag);
                if (enemy != null)
                {
                    var range = 10f; // stay in sight range
                    if (enemy.EnemiesInRangeOf.Count() > 1)
                    {
                        range = 14f; // other friendlies spotting
                    }

                    // if already within range, avoid damage
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, enemy.Position) < range * range)
                    {
                        if (AvoidDamageWhileLockedOn(commander, target, defensivePoint, frame, range, out action)) { return true; }
                    }

                    var avoidPoint = GetPositionFromRange(commander, enemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range);
                    // TODO: make sure the avoidpoint is same height or higher than enemy position so vision isn't lost
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            if (commander.LastLockOn == null || (frame - commander.LastLockOn.EndFrame) > (4.3 * SharkyOptions.FramesPerSecond))
            {
                return true;
            }
            return false;
        }

        bool AvoidDamageWhileLockedOn(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, float range, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = commander.UnitCalculation.EnemiesThreateningDamage;

            if (attacks.Count > 0)
            {
                var attack = attacks.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();  // enemies that are closest to being outranged

                if (commander.UnitCalculation.Range < range && commander.UnitCalculation.UnitTypeData.MovementSpeed <= attack.UnitTypeData.MovementSpeed)
                {
                    return false; // if we can't get out of range before we attack again don't bother running away
                }

                var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, range);
                if (avoidPoint != defensivePoint && avoidPoint != target)
                {
                    if (AvoidDeceleration(commander, avoidPoint, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }

                if (commander.RetreatPathFrame + 20 < frame)
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
                if (FollowPath(commander, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (frame == LastLockOnFrame) { return false; }
            if (commander.LastLockOn == null || (frame - commander.LastLockOn.EndFrame) > (4.3 * SharkyOptions.FramesPerSecond))
            {
                if (bestTarget != null)
                {
                    if (Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position) <= 49)
                    {
                        action = commander.Order(frame, Abilities.EFFECT_LOCKON, targetTag: bestTarget.Unit.Tag);
                        LastLockOnFrame = frame;
                        commander.LastLockOn = new LockOnData { StartFrame = frame, Tag = bestTarget.Unit.Tag, EndFrame = frame + (int)(14.3 * SharkyOptions.FramesPerSecond) };
                        return true;
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                        return true;
                    }
                }
            }
            
            return false;
        }

        protected override void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {       
            if (commander.LastLockOn != null && commander.LastLockOn.EndFrame > frame)
            {
                var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == commander.LastLockOn.Tag);
                if (enemy == null)
                {
                    commander.LastLockOn.EndFrame = frame;
                }
                else
                {
                    if (!enemy.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
                    {
                        commander.LastLockOn.EndFrame = frame;
                    }
                }

                if (commander.CommanderState == CommanderState.MaintainLockon && commander.LastLockOn.EndFrame <= frame)
                {
                    commander.CommanderState = CommanderState.None;
                }
                else if (commander.CommanderState != CommanderState.MaintainLockon && commander.LastLockOn.EndFrame > frame)
                {
                    commander.CommanderState = CommanderState.MaintainLockon;
                }
            }
        }

        protected override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();


            var attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u) && Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position) <= 7 * 7 && !u.Unit.BuffIds.Contains((uint)Buffs.LOCKON)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            // TODO: don't go attack units super far away if there are still units that can't attack this unit, but are close
            var outOfRangeAttacks = commander.UnitCalculation.NearbyEnemies.Where(enemyAttack => Vector2.DistanceSquared(enemyAttack.Position, commander.UnitCalculation.Position) > 7 * 7 && !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.LOCKON)
                && enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && AttackersFilter(commander, enemyAttack));

            attacks = outOfRangeAttacks.Where(enemyAttack => enemyAttack.EnemiesInRange.Count() > 0);
            if (attacks.Count() > 0)
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

            //attacks = outOfRangeAttacks.Where(enemyAttack => !enemyAttack.NearbyAllies.Any(u => u.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag) ||
            //            (DamageService.CanDamage(u, commander.UnitCalculation) && (Vector2.DistanceSquared(enemyAttack.Position, u.Position) < (u.Range * u.Range) ||
            //            Vector2.DistanceSquared(commander.UnitCalculation.Position, u.Position) < (u.Range * u.Range)))));
            attacks = outOfRangeAttacks;
            if (attacks.Count() > 0)
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

            if (bestAttack == null)
            {
                attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u)); 
                if (attacks.Count() > 0)
                {
                    bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                    if (bestAttack != null)
                    {
                        commander.BestTarget = bestAttack;
                        return bestAttack;
                    }
                }
            }

            if (!MapDataService.SelfVisible(target)) // if enemy main is unexplored, march to enemy main
            {
                var fakeMainBase = new Unit(commander.UnitCalculation.Unit);
                fakeMainBase.Pos = new Point { X = target.X, Y = target.Y, Z = 1 };
                fakeMainBase.Alliance = Alliance.Enemy;
                return new UnitCalculation(fakeMainBase, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            }
            var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20) && !e.Unit.BuffIds.Contains((uint)Buffs.LOCKON));
            if (unitsNearEnemyMain.Count() > 0 && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
            {
                attacks = unitsNearEnemyMain.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && AttackersFilter(commander, enemyAttack));
                if (attacks.Count() > 0)
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
    }
}
