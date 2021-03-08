using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class ZerglingMicroController : IndividualMicroController
    {
        public ZerglingMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health < 6 && bestTarget == null)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible)); // units that are in range right now

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
            }

            attacks = new List<UnitCalculation>(); // nearby units not in range right now
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation.Weapons, enemyAttack.Unit) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
                {
                    attacks.Add(enemyAttack);
                }
            }

            var safeAttacks = attacks.Where(a => a.Damage < commander.UnitCalculation.Unit.Health);
            if (safeAttacks.Count() > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, safeAttacks, existingAttackOrder);
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

            if (commander.UnitCalculation.Unit.Health < 6)
            {
                return null;
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

            if (!MapDataService.SelfVisible(target)) // if enemy main is unexplored, march to enemy main
            {
                var fakeMainBase = new Unit(commander.UnitCalculation.Unit);
                fakeMainBase.Pos = new Point { X = target.X, Y = target.Y, Z = 1 };
                fakeMainBase.Alliance = Alliance.Enemy;
                return new UnitCalculation(fakeMainBase, fakeMainBase, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            }
            var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20));
            if (unitsNearEnemyMain.Count() > 0 && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
            {
                attacks = new List<UnitCalculation>(); // enemies in the main enemy base
                foreach (var enemyAttack in unitsNearEnemyMain)
                {
                    if (enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation.Weapons, enemyAttack.Unit))
                    {
                        attacks.Add(enemyAttack);
                    }
                }
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
    }
}
