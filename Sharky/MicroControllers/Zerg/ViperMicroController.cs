namespace Sharky.MicroControllers.Zerg
{
    public class ViperMicroController : IndividualMicroController
    {
        private int lastBlindingCloudFrame = 0;
        private int lastParasiticBombFrame = 0;
        private int lastAbductFrame = 0;

        private Dictionary<UnitTypes, int> GroundAbductPriorities = new()
            {
                { UnitTypes.PROTOSS_DISRUPTOR, 200 },
                { UnitTypes.PROTOSS_COLOSSUS, 190 },
                { UnitTypes.PROTOSS_ARCHON, 180 },
                { UnitTypes.PROTOSS_HIGHTEMPLAR, 170 },
                { UnitTypes.PROTOSS_IMMORTAL, 160 },
                { UnitTypes.TERRAN_SIEGETANKSIEGED, 200 },
                { UnitTypes.TERRAN_THOR, 190 },
                { UnitTypes.TERRAN_THORAP, 180 },
                { UnitTypes.TERRAN_SIEGETANK, 170 },
                { UnitTypes.TERRAN_CYCLONE, 160 },
                { UnitTypes.TERRAN_GHOST, 150 },
                { UnitTypes.TERRAN_WIDOWMINEBURROWED, 140 },
                { UnitTypes.ZERG_LURKERMPBURROWED, 205 },
                { UnitTypes.ZERG_LURKERMP, 190 },
                { UnitTypes.ZERG_INFESTORBURROWED, 180 },
                { UnitTypes.ZERG_LURKERMPEGG, 175 },
                { UnitTypes.ZERG_INFESTOR, 170 },
                { UnitTypes.ZERG_RAVAGERBURROWED, 160 },
                { UnitTypes.ZERG_RAVAGER, 150 },
            };

        private Dictionary<UnitTypes, int> AirAbductPriorities = new()
            {
                { UnitTypes.PROTOSS_MOTHERSHIP, 200 },
                { UnitTypes.PROTOSS_TEMPEST, 190 },
                { UnitTypes.PROTOSS_CARRIER, 180 },
                { UnitTypes.PROTOSS_VOIDRAY, 170 },
                { UnitTypes.PROTOSS_ORACLE, 160 },
                { UnitTypes.PROTOSS_OBSERVER, 160 },
                { UnitTypes.TERRAN_BATTLECRUISER, 200 },
                { UnitTypes.TERRAN_RAVEN, 190 },
                { UnitTypes.TERRAN_BANSHEE, 180 },
                { UnitTypes.ZERG_VIPER, 200 },
                { UnitTypes.ZERG_BROODLORD, 190 },
                { UnitTypes.ZERG_BROODLORDCOCOON, 180 },
            };

        public ViperMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (Consume(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("consume");
                return true;
            }

            if (ParasiticBomb(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("parasitic");
                return true;
            }

            if (Abduct(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("abduct");
                return true;
            }

            action = BlindingCloud(commander, target, defensivePoint, groupCenter, bestTarget, frame);
            if (action != null)
            {
                return true;
            }

            return false;
        }

        private bool Consume(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy > commander.UnitCalculation.Unit.EnergyMax - 10)
            {
                return false;
            }

            bool wasConsuming = commander.LastAbility == Abilities.EFFECT_VIPERCONSUME || commander.LastAbility == Abilities.INVALID;
            if (commander.UnitCalculation.Unit.Energy < 75 || wasConsuming)
            {
                var buildings = ActiveUnitData.SelfUnits.Where(b => b.Value.Unit.Health >= 450 && b.Value.Attributes.Contains(SC2Attribute.Structure)).OrderBy(b => commander.UnitCalculation.Position.DistanceSquared(b.Value.Position));

                if (buildings.Any())
                {
                    action = commander.Order(frame, Abilities.EFFECT_VIPERCONSUME, null, buildings.First().Key);
                    return true;
                }
            }

            return false;
        }

        private List<SC2APIProtocol.Action> BlindingCloud(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame)
        {
            if (commander.UnitCalculation.Unit.Energy < 100 || frame < lastBlindingCloudFrame + 1)
            {
                return null;
            }

            var attacks = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && !enemyAttack.Unit.IsFlying && enemyAttack.Range > 3 && enemyAttack.EnemiesInRange.Any() &&
                        !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.BLINDINGCLOUD) && !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.BLINDINGCLOUDSTRUCTURE) &&
                                InRange(enemyAttack.Position, commander.UnitCalculation.Position, 11 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius));

            if (attacks.Count() > 0)
            {
                var bestAttack = GetBestAttack(commander.UnitCalculation, attacks, attacks, 3);
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = attacks.Where(u => u.DamageAir);
                    if (airAttackers.Count() > 0)
                    {
                        var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks, 3);
                        if (air != null)
                        {
                            bestAttack = air;
                        }
                    }
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    var groundAttackers = attacks.Where(u => u.DamageGround);
                    if (groundAttackers.Count() > 0)
                    {
                        var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks, 3);
                        if (ground != null)
                        {
                            bestAttack = ground;
                        }
                    }
                }

                if (bestAttack != null)
                {
                    var action = commander.Order(frame, Abilities.EFFECT_BLINDINGCLOUD, bestAttack);
                    lastBlindingCloudFrame = frame;
                    return action;
                }
            }

            return null;
        }

        private bool Abduct(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_ABDUCT))
            {
                lastParasiticBombFrame = frame;
                return true;
            }

            if (commander.UnitCalculation.Unit.Energy < 75 || frame < lastAbductFrame + 15)
            {
                return false;
            }

            var targets = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyUnit =>
                                InRange(enemyUnit.Position, commander.UnitCalculation.Position, 12 + enemyUnit.Unit.Radius + commander.UnitCalculation.Unit.Radius, 6));

            var alliesInRange = commander.UnitCalculation.NearbyAllies.Take(25).Where(ally =>
                InRange(ally.Position, commander.UnitCalculation.Position, 6));

            ulong bestAttack = 0;

            if (targets.Any())
            {
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = alliesInRange.Where(u => u.DamageAir);

                    if (airAttackers.Count() >= 3)
                    {
                        bestAttack = GetBestAbductUnit(targets, AirAbductPriorities) ?? 0;
                    }
                }

                if (bestAttack == 0 && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Attack))
                {
                    var groundAttackers = alliesInRange.Where(u => u.DamageGround);
                    if (groundAttackers.Count() >= 5)
                    {
                        bestAttack = GetBestAbductUnit(targets, GroundAbductPriorities) ?? 0;
                    }
                }

                if (bestAttack > 0)
                {
                    action = commander.Order(frame, Abilities.EFFECT_ABDUCT, targetTag: bestAttack);
                    lastAbductFrame = frame;
                    return true;
                }
            }

            return false;
        }

        private bool ParasiticBomb(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_PARASITICBOMB))
            {
                lastParasiticBombFrame = frame;
                return true;
            }

            if (commander.UnitCalculation.Unit.Energy < 125 || frame < lastParasiticBombFrame + 7)
            {
                return false;
            }

            var attacks = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && enemyAttack.Unit.IsFlying &&
                        !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.PARASITICBOMB) &&
                                InRange(enemyAttack.Position, commander.UnitCalculation.Position, 12 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius));

            if (attacks.Count() > 0)
            {
                var bestAttack = GetBestAttackUnit(commander.UnitCalculation, attacks, attacks, 3);
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    var airAttackers = attacks.Where(u => u.DamageAir);
                    if (airAttackers.Count() > 0)
                    {
                        var air = GetBestAttackUnit(commander.UnitCalculation, airAttackers, attacks, 3);
                        if (air > 0)
                        {
                            bestAttack = air;
                        }
                    }
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    var groundAttackers = attacks.Where(u => u.DamageGround);
                    if (groundAttackers.Count() > 0)
                    {
                        var ground = GetBestAttackUnit(commander.UnitCalculation, groundAttackers, attacks, 3);
                        if (ground > 0)
                        {
                            bestAttack = ground;
                        }
                    }
                }

                if (bestAttack > 0)
                {
                    action = commander.Order(frame, Abilities.EFFECT_PARASITICBOMB, targetTag: bestAttack);
                    lastParasiticBombFrame = frame;
                    return true;
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> splashableEnemies, float splashRadius, int threshold = 1)
        {
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
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < threshold)
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        private ulong? GetBestAbductUnit(IEnumerable<UnitCalculation> enemies, Dictionary<UnitTypes, int> abductPriorities)
        {
            ulong? bestAbductUnit = null;

            var enemy = enemies.OrderBy(x => AbductPriority((UnitTypes)x.Unit.UnitType, abductPriorities)).LastOrDefault();

            if (enemy is not null && abductPriorities.ContainsKey((UnitTypes)enemy.Unit.UnitType))
            {
                bestAbductUnit = enemy.Unit.Tag;
            }

            return bestAbductUnit;
        }

        private int AbductPriority(UnitTypes type, Dictionary<UnitTypes, int> abductPriorities)
        {
            return abductPriorities.TryGetValue(type, out var priority) ? priority : 0;
        }

        private ulong GetBestAttackUnit(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> splashableEnemies, float splashRadius, int threshold = 1)
        {
            var killCounts = new Dictionary<ulong, float>();
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
                killCounts[enemyAttack.Unit.Tag] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < threshold)
            {
                return 0;
            }
            return best.Key;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2Action> actions = null;

            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out actions))
            {
                return actions;
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
