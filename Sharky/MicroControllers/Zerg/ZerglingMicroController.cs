namespace Sharky.MicroControllers.Zerg
{
    public class ZerglingMicroController : IndividualMicroController
    {
        public ZerglingMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health < 6 && bestTarget == null)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            var baneling = commander.UnitCalculation.EnemiesInRangeOfAvoid.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_BANELING).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (baneling != null)
            {
                var distance = Vector2.DistanceSquared(commander.UnitCalculation.Position, baneling.Position);
                var closerLing = baneling.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING).Any(e => Vector2.DistanceSquared(baneling.Position, e.Position) < distance);
                if (closerLing)
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, baneling.Unit.Pos, target, defensivePoint, 5);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        public override float GetMovementSpeed(UnitCommander commander)
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

        public override List<SC2Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true)
        {
            if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                return NavigateToPoint(commander, target, defensivePoint, null, frame);
            }

            return base.Scout(commander, target, defensivePoint, frame, prioritizeVision);
        }

        protected override bool AttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (enemyAttack.Unit.IsHallucination) { return false; }
            return base.AttackersFilter(commander, enemyAttack);
        }

        protected override bool GroundAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (enemyAttack.Unit.IsHallucination) { return false; }
            return base.GroundAttackersFilter(commander, enemyAttack);
        }

        bool BonusDamageToSelf(UnitCommander commander, UnitCalculation enemy)
        {
            if (enemy.DamageGround && enemy.Weapon != null && enemy.Weapon.DamageBonus.Any(b => b.Attribute == SC2APIProtocol.Attribute.Light))
            {
                return true;
            }
            return false;
        }

        protected override UnitCalculation GetBestTargetFromListWinGround(UnitCommander commander, IEnumerable<UnitCalculation> attacks, UnitOrder existingAttackOrder, Weapon weapon)
        {
            var groundAttackers = attacks.Where(u => u.DamageGround && u.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && (!u.UnitClassifications.HasFlag(UnitClassification.Worker) || u.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag)) && GroundAttackersFilter(commander, u)).Where(e => !BonusDamageToSelf(commander, e));
            if (groundAttackers.Any())
            {
                var bestDpsReduction = GetBestDpsReduction(commander, weapon, groundAttackers, attacks);

                if (existingAttackOrder != null && bestDpsReduction != null)
                {
                    var existingReduction = groundAttackers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                    if (existingReduction == null && commander.BestTarget != null)
                    {
                        existingReduction = groundAttackers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                    }
                    if (existingReduction != null)
                    {
                        var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                        var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                        if (existing * 1.25 > best)
                        {
                            return existingReduction; // just keep attacking the same unit
                        }
                    }
                }

                if (bestDpsReduction != null)
                {
                    return bestDpsReduction;
                }
            }

            return base.GetBestTargetFromListWinGround(commander, attacks, existingAttackOrder, weapon);
        }

        protected override UnitCalculation GetBestTargetFromListThreats(UnitCommander commander, IEnumerable<UnitCalculation> attacks, UnitOrder existingAttackOrder, Weapon weapon, IEnumerable<UnitCalculation> threats)
        {
            var specificThreats = threats.Where(e => !BonusDamageToSelf(commander, e));
            var bestDpsReduction = GetBestDpsReduction(commander, weapon, specificThreats, attacks);
            if (existingAttackOrder != null && bestDpsReduction != null)
            {
                var existingReduction = threats.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                if (commander.BestTarget != null)
                {
                    var existing = threats.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                    if (existing != null)
                    {
                        existingReduction = existing;
                    }
                }
                if (existingReduction != null)
                {
                    var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                    var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                    if (existing * 1.25 > best)
                    {
                        return existingReduction; // just keep attacking the same unit
                    }
                }
            }
            if (bestDpsReduction != null)
            {
                return bestDpsReduction;
            }

            return base.GetBestTargetFromListThreats(commander, attacks, existingAttackOrder, weapon, threats);
        }
    }
}
