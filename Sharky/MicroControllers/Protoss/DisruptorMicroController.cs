using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class DisruptorMicroController : IndividualMicroController
    {
        int PurificationNovaRange = 13;

        public DisruptorMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.AbilityOffCooldown(Abilities.EFFECT_PURIFICATIONNOVA, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

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

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < PurificationNovaRange * PurificationNovaRange))
            {
                return false;
            }

            var attacks = new List<UnitCalculation>();
            var center = new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y);

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (!enemyAttack.Unit.IsFlying && enemyAttack.Damage > 0 && InRange(enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, PurificationNovaRange + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius)) // TODO: do actual pathing to see if the shot can make it there, if a wall is in the way it can't
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetPurificationNovaDamage(a.Unit, SharkyUnitData.UnitData[(UnitTypes)a.Unit.UnitType]) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD)).OrderByDescending(u => u.Dps);
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
                        action = commander.Order(frame, Abilities.EFFECT_PURIFICATIONNOVA, bestAttack);
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander)
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

        private Point2D GetBestAttack(UnitCalculation potentialAttack, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
        {
            float splashRadius = 1.5f;
            var killCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemies)
            {
                int killCount = 0;
                foreach (var splashedEnemy in splashableEnemies)
                {
                    if (Vector2.DistanceSquared(new Vector2(splashedEnemy.Unit.Pos.X, splashedEnemy.Unit.Pos.Y), new Vector2(enemyAttack.Unit.Pos.X, enemyAttack.Unit.Pos.Y)) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        if (splashedEnemy.Unit.Health + splashedEnemy.Unit.Shield < GetPurificationNovaDamage(splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]))
                        {
                            killCount++;
                        }
                    }
                }
                foreach (var splashedAlly in potentialAttack.NearbyAllies)
                {
                    if (Vector2.DistanceSquared(new Vector2(splashedAlly.Unit.Pos.X, splashedAlly.Unit.Pos.Y), new Vector2(enemyAttack.Unit.Pos.X, enemyAttack.Unit.Pos.Y)) < (splashedAlly.Unit.Radius + splashRadius) * (splashedAlly.Unit.Radius + splashRadius))
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

            if (best.Value < 3) // only attack if going to kill >= 3 units
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }
    }
}
