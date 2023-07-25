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

        protected List<UnitTypes> TargetPriorityList { get; set; }


        public CycloneMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 2f; // TODO: may need to turn off autolock on
            LastLockOnFrame = -1;

            TargetPriorityList = new List<UnitTypes> {
                UnitTypes.PROTOSS_WARPPRISM, UnitTypes.PROTOSS_WARPPRISMPHASING, UnitTypes.TERRAN_MEDIVAC, UnitTypes.ZERG_OVERLORDTRANSPORT
            };
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AutoCastToggled)
            {
                action = commander.ToggleAutoCast(Abilities.EFFECT_LOCKON);
                commander.AutoCastToggled = true;
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
                var attack = attacks.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();  // enemies that are closest to being outranged

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

                if (commander.RetreatPathFrame + 2 < frame)
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
                if (bestTarget != null && bestTarget.FrameLastSeen == frame && bestTarget.Unit.Tag != commander.UnitCalculation.Unit.Tag && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    if (Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position) <= 49)
                    {
                        ChatService.Tag("a_lockon");
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
            else
            {
                base.UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);
            }
        }

        protected override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {

            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            // damaging units in priority in range or not
            // non damaging units in priority in range, then not in range
            // other stuff in range, then not in range

            var priorityAttacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.DisplayType == DisplayType.Visible && TargetPriorityList.Contains((UnitTypes)e.Unit.UnitType));
            var priorityDamagers = priorityAttacks.Where(e => e.Damage > 0);
            var priorityOther = priorityAttacks.Where(e => e.Damage == 0);

            var attacks = commander.UnitCalculation.NearbyEnemies.Where(e => priorityDamagers.Any(p => p.Unit.Tag == e.Unit.Tag)).Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u) && Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position) <= 7 * 7 && (u.SimulatedHitpoints > 200 || !u.Unit.BuffIds.Contains((uint)Buffs.LOCKON)));

            UnitCalculation bestAttack = null;
            if (attacks.Count() > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
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
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = priorityDamagers;
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            // defend proxy units/buildings
            attacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.EnemiesInRange.Count() > 0);
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityOther.Any(p => p.Unit.Tag == e.Unit.Tag));
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityOther.Any(p => p.Unit.Tag == e.Unit.Tag));
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = priorityOther;
            if (attacks.Count() > 0)
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            if (bestAttack == null)
            {
                return base.GetBestTarget(commander, target, frame);
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }
    }
}
