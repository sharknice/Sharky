using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class ViperMicroController : IndividualMicroController
    {
        private int lastBlindingCloudFrame = 0;
        private int lastParasiticBombFrame = 0;

        public ViperMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            if (ParasiticBomb(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                return true;
            }

            action = BlindingCloud(commander, target, defensivePoint, groupCenter, bestTarget, frame);
            if (action != null)
            {
                return true;
            }

            // TODO: parasitic bomb
            // TODO: abduct

            return false;
        }

        private List<SC2APIProtocol.Action> BlindingCloud(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame)
        {
            if (commander.UnitCalculation.Unit.Energy < 100 || frame < lastBlindingCloudFrame + 1)
            {
                return null;
            }

            var attacks = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && !enemyAttack.Unit.IsFlying && enemyAttack.Range > 3 && enemyAttack.EnemiesInRange.Any() &&
                        !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.BLINDINGCLOUD) && !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.BLINDINGCLOUDSTRUCTURE) &&
                                InRange(enemyAttack.Position, commander.UnitCalculation.Position, 11 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius));

            if (attacks.Count() > 0)
            {
                var bestAttack = GetBestAttack(commander.UnitCalculation, attacks, attacks, 2);
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = attacks.Where(u => u.DamageAir);
                    if (airAttackers.Count() > 0)
                    {
                        var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks, 2);
                        if (air != null)
                        {
                            bestAttack = air;
                        }
                    }
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    var groundAttackers = attacks.Where(u => u.DamageGround);
                    if (groundAttackers.Count() > 0)
                    {
                        var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks, 2);
                        if (ground != null)
                        {
                            bestAttack = ground;
                        }
                    }
                }

                if (bestAttack != null)
                {
                    var action = commander.Order(frame, Abilities.EFFECT_BLINDINGCLOUD, bestAttack);
                    lastBlindingCloudFrame = frame;
                    return action;
                }
            }

            return null;
        }

        private bool ParasiticBomb(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_PARASITICBOMB))
            {
                lastParasiticBombFrame = frame;
                return true;
            }

            if (commander.UnitCalculation.Unit.Energy < 125 || frame < lastParasiticBombFrame + 15)
            {
                return false;
            }

            var attacks = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && enemyAttack.Unit.IsFlying && 
                        !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.PARASITICBOMB) &&
                                InRange(enemyAttack.Position, commander.UnitCalculation.Position, 12 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius));

            if (attacks.Count() > 0)
            {
                var bestAttack = GetBestAttackUnit(commander.UnitCalculation, attacks, attacks, 3);
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = attacks.Where(u => u.DamageAir);
                    if (airAttackers.Count() > 0)
                    {
                        var air = GetBestAttackUnit(commander.UnitCalculation, airAttackers, attacks, 3);
                        if (air > 0)
                        {
                            bestAttack = air;
                        }
                    }
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    var groundAttackers = attacks.Where(u => u.DamageGround);
                    if (groundAttackers.Count() > 0)
                    {
                        var ground = GetBestAttackUnit(commander.UnitCalculation, groundAttackers, attacks, 3);
                        if (ground > 0)
                        {
                            bestAttack = ground;
                        }
                    }
                }

                if (bestAttack > 0)
                {
                    action = commander.Order(frame, Abilities.EFFECT_PARASITICBOMB, targetTag: bestAttack);
                    lastParasiticBombFrame = frame;
                    return true;
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> splashableEnemies, float splashRadius, int threshold = 1)
        {
            var killCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemies)
            {
                int killCount = 0;
                foreach (var splashedEnemy in splashableEnemies)
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        killCount++;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < threshold)
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        private ulong GetBestAttackUnit(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> splashableEnemies, float splashRadius, int threshold = 1)
        {
            var killCounts = new Dictionary<ulong, float>();
            foreach (var enemyAttack in enemies)
            {
                int killCount = 0;
                foreach (var splashedEnemy in splashableEnemies)
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        killCount++;
                    }
                }
                killCounts[enemyAttack.Unit.Tag] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < threshold)
            {
                return 0;
            }
            return best.Key;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<Action> actions = null;

            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out actions))
            {
                return actions;
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
