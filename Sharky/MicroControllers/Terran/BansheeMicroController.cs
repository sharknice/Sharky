namespace Sharky.MicroControllers.Terran
{
    public class BansheeMicroController : IndividualMicroController
    {
        public BansheeMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled, 2)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEECLOAK))
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK)) // already cloaked
                {
                    if (!commander.UnitCalculation.NearbyEnemies.Any() && commander.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2Attribute.Structure)))
                    {
                        action = commander.Order(frame, Abilities.BEHAVIOR_CLOAKOFF);
                        return true;
                    }

                    return false;
                }

                if (commander.UnitCalculation.Unit.Energy > 25 && (commander.UnitCalculation.EnemiesInRangeOf.Any() || commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR))) // if enemies can hit it, cloak
                {
                    TagService.TagAbility("banshee_cloak");
                    action = commander.Order(frame, Abilities.BEHAVIOR_CLOAKON);
                    return true;
                }
            }

            return false;
        }

        public override List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (PreOffenseOrder(commander, target, defensivePoint, null, null, frame, out action)) { return action; }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action)) { return action; }
            if (AvoidEnemiesThreateningDamage(commander, target, defensivePoint, frame, false, out action)) { return action; }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            var bestTarget = GetBestHarassTarget(commander, target);

            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame) && bestTarget != null && bestTarget.UnitClassifications.Contains(UnitClassification.Worker) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            if (!CloakedAndUndetected(commander))
            {
                var formation = GetDesiredFormation(commander);
                if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }
            }
            else if (bestTarget != null && bestTarget.UnitClassifications.Contains(UnitClassification.Worker))
            {
                return MoveToTarget(commander, bestTarget.Unit.Pos.ToPoint2D(), frame);
            }

            return MoveToTarget(commander, target, frame);
        }

        public override UnitCalculation GetBestHarassTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && u.UnitClassifications.Contains(UnitClassification.Worker)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Any())
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData));
                if (oneShotKills.Any())
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

            attacks = commander.UnitCalculation.NearbyEnemies.Where(enemyAttack => enemyAttack.Unit.DisplayType != DisplayType.Hidden && enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius)); // nearby units not in range right now
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

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEESPEED))
            {
                return 5.25f;
            }
            return base.GetMovementSpeed(commander);
        }

        protected bool CloakedAndUndetected(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)) { return false; }
            return (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) || (commander.UnitCalculation.Unit.Energy > 50 && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEECLOAK))) && !MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos);
        }
    }
}
