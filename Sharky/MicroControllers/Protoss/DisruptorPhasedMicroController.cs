using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class DisruptorPhasedMicroController : IndividualMicroController
    {
        int PurificationNovaRange = 13;
        float PurificationNovaSpeed = 5.95f;

        public DisruptorPhasedMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (PurificationNova(commander, frame, out action)) { return action; }

            return null;
        }

        private bool PurificationNova(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (!enemyAttack.Unit.IsFlying && InRange(enemyAttack.Position, commander.UnitCalculation.Position, ((PurificationNovaSpeed / SharkyOptions.FramesPerSecond) * commander.UnitCalculation.Unit.BuffDurationRemain) + 3f + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius)) // TODO: do actual pathing to see if the shot can make it there, if a wall is in the way it can't
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

                    if (bestAttack != null)
                    {
                        action = commander.Order(frame, Abilities.MOVE, bestAttack);
                        return true;
                    }
                }
            }

            return AvoidFriendlyFire(commander, frame, out action);
        }

        private bool AvoidFriendlyFire(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var closest = commander.UnitCalculation.NearbyAllies.OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closest != null)
            {
                var avoidPoint = GetPositionFromRange(commander, closest.Unit.Pos, commander.UnitCalculation.Unit.Pos, 3f + commander.UnitCalculation.Unit.Radius + closest.Unit.Radius + .5f);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }
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

        private Point2D GetBestAttack(UnitCalculation potentialAttack, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
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
                        if (splashedEnemy.Unit.Health + splashedEnemy.Unit.Shield < GetPurificationNovaDamage(splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]))
                        {
                            killCount++;
                        }
                    }
                }
                foreach (var splashedAlly in potentialAttack.NearbyAllies.Where(a => !a.Unit.IsFlying && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + splashRadius) * (splashedAlly.Unit.Radius + splashRadius))
                    {
                        if (splashedAlly.Unit.Health + splashedAlly.Unit.Shield < GetPurificationNovaDamage(splashedAlly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedAlly.Unit.UnitType]))
                        {
                            killCount--;
                        }
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < 0) // don't kill own units
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }
    }
}
