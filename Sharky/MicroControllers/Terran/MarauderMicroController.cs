
namespace Sharky.MicroControllers.Terran
{
    public class MarauderMicroController : StimableMicroController
    {
        public MarauderMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (Stiming(commander))
            {
                return SharkyOptions.FramesPerSecond * 0.71f;
            }

            return base.GetWeaponCooldown(commander, enemy);       
        }

        public override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PUNISHERGRENADES))
            {
                var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

                var attacks = commander.UnitCalculation.EnemiesInRange.Where(e => AttackersFilter(commander, e));
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Any())
                {
                    if (!commander.UnitCalculation.Weapons.Any(weapon => weapon.DamageBonus.Any()) || commander.UnitCalculation.Weapons.Any(weapon => weapon.DamageBonus.Any(b => oneShotKills.Any(o => o.Attributes.Any(a => a == b.Attribute)))))
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
                }

                var threats = commander.UnitCalculation.EnemiesInRangeOfAvoid.Where(e => !e.Unit.BuffIds.Contains((uint)Buffs.SLOW) && AttackersFilter(commander, e)).OrderBy(e => Vector2.Distance(commander.UnitCalculation.Position, e.Position) - e.Range);
                var inRangeThreat = threats.FirstOrDefault(e => commander.UnitCalculation.EnemiesInRange.Any(ir => ir.Unit.Tag == e.Unit.Tag));
                if (inRangeThreat != null)
                {
                    return inRangeThreat;
                }

                if (threats.FirstOrDefault() != null)
                {
                    return threats.FirstOrDefault();
                }
            }

            return base.GetBestTarget(commander, target, frame);
        }

        public override bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            if (!(commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && !SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PUNISHERGRENADES)) && !(formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count > 5))
            {
                if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (MoveFromBeingClosest(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            var closest = commander.UnitCalculation.EnemiesInRange.Where(e => DamageService.CanDamage(e, commander.UnitCalculation)).OrderBy(e => Vector2.Distance(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closest != null && closest.Damage > 0 && closest.Range < commander.UnitCalculation.Range && (closest.UnitTypeData.MovementSpeed <= commander.UnitCalculation.UnitTypeData.MovementSpeed || closest.Unit.UnitType == (uint)UnitTypes.PROTOSS_ARCHON))
            {
                var avoidPoint = GetPositionFromRange(commander, closest.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range);
                if (MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) || MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(closest.Unit.Pos))
                {
                }
                else
                {
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }
    }
}
