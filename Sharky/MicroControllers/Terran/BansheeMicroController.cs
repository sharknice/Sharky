using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    class BansheeMicroController : IndividualMicroController
    {
        public BansheeMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {

        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEECLOAK))
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK)) // already cloaked
                {
                    return false;
                }

                if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0) // if enemies can hit it, cloak
                {
                    action = commander.Order(frame, Abilities.BEHAVIOR_CLOAKON);
                    return true;
                }
            }

            return false;
        }

        public List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (PreOffenseOrder(commander, target, defensivePoint, null, null, frame, out action)) { return action; }

            if (MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos))
            {
                if (commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageAir) > 0)
                {
                    if (commander.RetreatPathFrame + 20 < frame)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(target.X, target.Y, commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, frame);
                        commander.RetreatPathFrame = frame;
                    }

                    if (FollowPath(commander, frame, out action)) { return action; }
                }

                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return action;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return action;
                }
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        public List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var bestTarget = GetBestHarassTarget(commander, target);

            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander) && commander.UnitCalculation.EnemiesInRange.Count() > 0)
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            return NavigateToPoint(commander, target, defensivePoint, null, frame);
        }

        UnitCalculation GetBestHarassTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && u.UnitClassifications.Contains(UnitClassification.Worker))); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData));
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
                if (bestAttack != null && bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = new List<UnitCalculation>(); // nearby units not in range right now
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (enemyAttack.Unit.DisplayType != DisplayType.Hidden && enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
                {
                    attacks.Add(enemyAttack);
                }
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

            commander.BestTarget = bestAttack;
            return bestAttack;
        }
    }
}
