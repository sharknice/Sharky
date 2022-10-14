using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class ZerglingMicroController : IndividualMicroController
    {
        public ZerglingMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
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

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Any())
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
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.NearbyEnemies.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && !AvoidedUnitTypes.Contains((UnitTypes)enemyAttack.Unit.UnitType) && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius));

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

            if (attacks.Any())
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
                return new UnitCalculation(fakeMainBase, 0, SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(fakeMainBase.Pos), frame);
            }
            var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => !AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType) && e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20));
            if (unitsNearEnemyMain.Any() && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
            {
                attacks = unitsNearEnemyMain.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack)); // enemies in the main enemy base
                if (attacks.Any())
                {
                    var bestMainAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                    if (bestMainAttack != null && (bestMainAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestMainAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || bestMainAttack.UnitClassifications.Contains(UnitClassification.Worker)))
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

        protected override float GetMovementSpeed(UnitCommander commander)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.ZERGLINGMOVEMENTSPEED))
            {
                if (commander.UnitCalculation.IsOnCreep)
                {
                    return 8.55f;
                }
                return 6.58f;
            }

            if (commander.UnitCalculation.IsOnCreep)
            {
                return 5.37f;
            }

            return base.GetMovementSpeed(commander);
        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.ZERGLINGATTACKSPEED))
            {
                return SharkyOptions.FramesPerSecond * 0.35f;
            }

            return base.GetWeaponCooldown(commander, enemy);
        }

        public override List<Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false)
        {
            if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                return NavigateToPoint(commander, target, defensivePoint, null, frame);
            }

            return base.Scout(commander, target, defensivePoint, frame, prioritizeVision);
        }
    }
}
