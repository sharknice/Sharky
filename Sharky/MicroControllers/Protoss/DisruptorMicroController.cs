using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class DisruptorMicroController : IndividualMicroController
    {
        int PurificationNovaRange = 13;
        private int lastPurificationFrame = 0;

        public DisruptorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_PURIFICATIONNOVA, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < PurificationNovaRange * PurificationNovaRange))
            {
                return false;
            }

            if (lastPurificationFrame >= frame - 5)
            {
                return false;
            }

            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (!enemyAttack.Unit.IsFlying && enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && 
                    InRange(enemyAttack.Position, commander.UnitCalculation.Position, PurificationNovaRange + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius)) // TODO: do actual pathing to see if the shot can make it there, if a wall is in the way it can't
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.OrderBy(a => GetPurificationNovaDamage(a.Unit, SharkyUnitData.UnitData[(UnitTypes)a.Unit.UnitType])).ThenByDescending(u => u.Dps);
                if (oneShotKills.Count() > 0)
                {
                    var bestAttack = GetBestAttack(commander.UnitCalculation, oneShotKills, attacks);
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                    {
                        var airAttackers = oneShotKills.Where(u => u.DamageAir);
                        if (airAttackers.Count() > 0)
                        {
                            var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks);
                            if (air != null)
                            {
                                bestAttack = air;
                            }
                        }
                    }
                    else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                    {
                        var groundAttackers = oneShotKills.Where(u => u.DamageGround);
                        if (groundAttackers.Count() > 0)
                        {
                            var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks);
                            if (ground != null)
                            {
                                bestAttack = ground;
                            }
                        }
                    }
                    else
                    {
                        if (oneShotKills.Count() > 0)
                        {
                            var any = GetBestAttack(commander.UnitCalculation, oneShotKills, attacks);
                            if (any != null)
                            {
                                bestAttack = any;
                            }
                        }
                    }

                    if (bestAttack != null)
                    {
                        action = commander.Order(frame, Abilities.EFFECT_PURIFICATIONNOVA, bestAttack);
                        lastPurificationFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        private float GetPurificationNovaDamage(Unit unit, UnitTypeData unitTypeData)
        {
            float bonusDamage = 0;
            if (unit.Shield > 0)
            {
                bonusDamage = 55;
            }

            return 145 + bonusDamage - unitTypeData.Armor; // TODO: armor upgrades
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
        {
            float splashRadius = 1.5f;
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
                foreach (var splashedAlly in unitCalculation.NearbyAllies.Where(a => !a.Unit.IsFlying && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + splashRadius) * (splashedAlly.Unit.Radius + splashRadius))
                    {
                        killCount--;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < 3 && !unitCalculation.NearbyAllies.Any(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && u.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR)) // only attack if going to kill 3+ units or no army to help defend
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
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
