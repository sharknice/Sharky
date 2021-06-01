using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class HighTemplarMicroController : IndividualMicroController
    {
        private int StormRange = 9;
        private double StormRadius = 1.5;
        private double FeedbackRangeSquared = 121; // actually range 10, but give an extra 1 range to get first feedback in
        private int lastStormFrame = 0;

        public HighTemplarMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled) 
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {

        }
        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.Shield < 20)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (Storm(commander, frame, out action))
            {
                return true;
            }

            if (Feedback(commander, frame, out action))
            {
                return true;
            }

            if (Merge(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool Merge(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy > 40 || commander.UnitCalculation.NearbyEnemies.Count() == 0)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON))
            {
                return true;
            }

            var otherHighTemplar = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR && a.Unit.Energy <= 40);

            if (otherHighTemplar.Count() > 0)
            {
                var target = otherHighTemplar.OrderBy(o => Vector2.DistanceSquared(o.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (target != null)
                {
                    var merge = commander.Merge(target.Unit.Tag);
                    if (merge != null)
                    {
                        action = new List<Action> { merge };
                    }
                    return true;
                }
            }

            return false;
        }

        bool Feedback(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            var vector = commander.UnitCalculation.Position;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Energy > 1 && e.Unit.DisplayType == DisplayType.Visible && Vector2.DistanceSquared(e.Position, vector) < FeedbackRangeSquared).OrderByDescending(e => e.Unit.Energy);

            var oneShotKill = enemiesInRange.Where(e => e.Unit.Energy * .5 > e.Unit.Health + e.Unit.Shield).FirstOrDefault();
            if (oneShotKill != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, oneShotKill.Unit.Tag);
                return true;
            }
            var target = enemiesInRange.FirstOrDefault();
            if (target != null && target.Unit.Energy > 50)
            {
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, target.Unit.Tag);
                return true;
            }

            return false;
        }

        bool Storm(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.AbilityOffCooldown(Abilities.EFFECT_PSISTORM, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return true; // don't do anything until it storms
            }

            if (commander.UnitCalculation.Unit.Energy < 75 || !SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PSISTORMTECH))
            {
                return false;
            }

            if (lastStormFrame >= frame - 5)
            {
                return false;
            }

            var enemies = commander.UnitCalculation.NearbyEnemies.Where(a => !a.Attributes.Contains(Attribute.Structure) && !a.Unit.BuffIds.Contains((uint)Buffs.PSISTORM)).OrderBy(u => u.Unit.Health);
            if (enemies.Count() > 2)
            {
                var bestAttack = GetBestAttack(commander.UnitCalculation, enemies);
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = enemies.Where(u => u.DamageAir);
                    if (airAttackers.Count() > 0)
                    {
                        var air = GetBestAttack(commander.UnitCalculation, airAttackers);
                        if (air != null)
                        {
                            bestAttack = air;
                        }
                    }
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    var groundAttackers = enemies.Where(u => u.DamageGround);
                    if (groundAttackers.Count() > 0)
                    {
                        var ground = GetBestAttack(commander.UnitCalculation, groundAttackers);
                        if (ground != null)
                        {
                            bestAttack = ground;
                        }
                    }
                }
                else
                {
                    if (enemies.Count() > 0)
                    {
                        var any = GetBestAttack(commander.UnitCalculation, enemies);
                        if (any != null)
                        {
                            bestAttack = any;
                        }
                    }
                }

                if (bestAttack != null)
                {
                    action = commander.Order(frame, Abilities.EFFECT_PSISTORM, bestAttack);
                    lastStormFrame = frame;
                    return true;
                }
            }

            return false;
        }

        private Point2D GetBestAttack(UnitCalculation potentialAttack, IEnumerable<UnitCalculation> enemies)
        {
            var killCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemies)
            {
                int killCount = 0;
                foreach (var splashedEnemy in enemyAttack.NearbyAllies.Where(a => !a.Attributes.Contains(Attribute.Structure) && !a.Unit.BuffIds.Contains((uint)Buffs.PSISTORM)))
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + StormRadius) * (splashedEnemy.Unit.Radius + StormRadius))
                    {
                        killCount++;
                    }
                }
                foreach (var splashedAlly in potentialAttack.NearbyAllies.Where(a => !a.Attributes.Contains(Attribute.Structure)))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + StormRadius) * (splashedAlly.Unit.Radius + StormRadius))
                    {
                        killCount-=3;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < 3) // only attack if going to kill >= 3 units
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }
    }
}
