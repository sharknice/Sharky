namespace Sharky.MicroControllers.Zerg
{
    public class InfestorMicroController : IndividualMicroController
    {
        private int lastFungalFrame = 0;
        private int lastNeuralFrame = 0;
        //private int lastShroudFrame = 0;

        private Dictionary<UnitTypes, int> GroundNeuralPriorities = new()
            {
                { UnitTypes.PROTOSS_DISRUPTOR, 200 },
                { UnitTypes.PROTOSS_HIGHTEMPLAR, 170 },
                { UnitTypes.PROTOSS_COLOSSUS, 190 },
                { UnitTypes.PROTOSS_ARCHON, 180 },
                { UnitTypes.PROTOSS_IMMORTAL, 170 },
                { UnitTypes.TERRAN_GHOST, 210 },
                { UnitTypes.TERRAN_SIEGETANKSIEGED, 200 },
                { UnitTypes.TERRAN_THOR, 190 },
                { UnitTypes.TERRAN_THORAP, 180 },
                { UnitTypes.TERRAN_SIEGETANK, 170 },
                { UnitTypes.TERRAN_CYCLONE, 160 },
                { UnitTypes.TERRAN_WIDOWMINEBURROWED, 140 },
                { UnitTypes.ZERG_INFESTOR, 200 },
                { UnitTypes.ZERG_INFESTORBURROWED, 190 },
                { UnitTypes.ZERG_LURKERMPBURROWED, 180 },
                { UnitTypes.ZERG_LURKERMP, 170 },
            };

        private Dictionary<UnitTypes, int> AirNeuralPriorities = new()
            {
                { UnitTypes.PROTOSS_TEMPEST, 190 },
                { UnitTypes.PROTOSS_CARRIER, 180 },
                { UnitTypes.PROTOSS_VOIDRAY, 170 },
                { UnitTypes.PROTOSS_ORACLE, 160 },
                { UnitTypes.TERRAN_BATTLECRUISER, 200 },
                { UnitTypes.TERRAN_RAVEN, 190 },
                { UnitTypes.TERRAN_BANSHEE, 180 },
                { UnitTypes.ZERG_VIPER, 200 },
                { UnitTypes.ZERG_BROODLORD, 190 },
            };

        public InfestorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW))
            {
                if (commander.UnitCalculation.NearbyEnemies.Any() && commander.UnitCalculation.Unit.Energy < 75)
                {
                    action = commander.Order(frame, Abilities.BURROWDOWN);
                    return true;
                }
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        private ulong? GetBestNeuralUnit(IEnumerable<UnitCalculation> enemies, Dictionary<UnitTypes, int> neuralPriorities)
        {
            ulong? bestNeuralUnit = null;

            var enemy = enemies.OrderBy(x => NeuralPriority((UnitTypes)x.Unit.UnitType, neuralPriorities)).LastOrDefault();

            if (enemy is not null && neuralPriorities.ContainsKey((UnitTypes)enemy.Unit.UnitType))
            {
                bestNeuralUnit = enemy.Unit.Tag;
            }

            return bestNeuralUnit;
        }

        private int NeuralPriority(UnitTypes type, Dictionary<UnitTypes, int> neuralPriorities)
        {
            return neuralPriorities.TryGetValue(type, out var priority) ? priority : 0;
        }

        protected bool NeuralAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            int range = 8;

            if (commander.UnitCalculation.Unit.Energy < 100)
            {
                return false;
            }

            if (lastNeuralFrame >= frame - 5)
            {
                return false;
            }

            var targets = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyUnit =>
                                InRange(enemyUnit.Position, commander.UnitCalculation.Position, range + enemyUnit.Unit.Radius + commander.UnitCalculation.Unit.Radius));

            if (targets.Any())
            {
                ulong bestAttack = 0;

                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                {
                    bestAttack = GetBestNeuralUnit(targets, GroundNeuralPriorities) ?? 0;
                }
                else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                {
                    bestAttack = GetBestNeuralUnit(targets, AirNeuralPriorities) ?? 0;
                }

                if (bestAttack == 0)
                {
                    bestAttack = GetBestNeuralUnit(targets, AirNeuralPriorities) ?? 0;
                    if (bestAttack == 0)
                    {
                        bestAttack = GetBestNeuralUnit(targets, GroundNeuralPriorities) ?? 0;
                    }
                }

                if (bestAttack > 0)
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.EFFECT_NEURALPARASITE, targetTag: bestAttack);
                    lastNeuralFrame = frame;
                    return true;
                }
            }

            return false;
        }

        protected bool ShroudAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;
            // todo: implement this
            return false;

            //int range = 9;

            //if (commander.UnitCalculation.Unit.Energy < 75)
            //{
            //    return false;
            //}

            //if (lastShroudFrame >= frame - 10)
            //{
            //    return false;
            //}

            //var attacks = new List<UnitCalculation>();
            //var center = commander.UnitCalculation.Position;

            //foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies.Take(25))
            //{
            //    if (enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && !enemyAttack.Attributes.Contains(SC2Attribute.Structure) && !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.FUNGALGROWTH) &&
            //        InRange(enemyAttack.Position, commander.UnitCalculation.Position, 10 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
            //    {
            //        attacks.Add(enemyAttack);
            //    }
            //}

            //if (attacks.Count > 0)
            //{
            //    var victims = attacks.OrderByDescending(u => u.Dps);
            //    if (victims.Any())
            //    {
            //        var bestAttack = GetBestAttack(commander.UnitCalculation, victims, attacks);
            //        if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
            //        {
            //            var airAttackers = victims.Where(u => u.DamageAir);
            //            if (airAttackers.Any())
            //            {
            //                var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks);
            //                if (air != null)
            //                {
            //                    bestAttack = air;
            //                }
            //            }
            //        }
            //        else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
            //        {
            //            var groundAttackers = victims.Where(u => u.DamageGround);
            //            if (groundAttackers.Any())
            //            {
            //                var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks);
            //                if (ground != null)
            //                {
            //                    bestAttack = ground;
            //                }
            //            }
            //        }
            //        else
            //        {
            //            if (victims.Any())
            //            {
            //                var any = GetBestAttack(commander.UnitCalculation, victims, attacks);
            //                if (any != null)
            //                {
            //                    bestAttack = any;
            //                }
            //            }
            //        }

            //        if (bestAttack != null)
            //        {
            //            action = commander.Order(frame, Abilities.EFFECT_FUNGALGROWTH, bestAttack);
            //            lastShroudFrame = frame;
            //            return true;
            //        }
            //    }
            //}

            //return false;
        }

        protected bool FungalAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            if (lastFungalFrame >= frame - 10)
            {
                return false;
            }

            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies.Take(25))
            {
                if (enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && !enemyAttack.Attributes.Contains(SC2Attribute.Structure) && !enemyAttack.Unit.BuffIds.Contains((uint)Buffs.FUNGALGROWTH) &&
                    InRange(enemyAttack.Position, commander.UnitCalculation.Position, 10 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius))
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var victims = attacks.OrderByDescending(u => u.Dps);
                if (victims.Any())
                {
                    var bestAttack = GetBestAttack(commander.UnitCalculation, victims, attacks);
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                    {
                        var airAttackers = victims.Where(u => u.DamageAir);
                        if (airAttackers.Any())
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
                        var groundAttackers = victims.Where(u => u.DamageGround);
                        if (groundAttackers.Any())
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
                        if (victims.Any())
                        {
                            var any = GetBestAttack(commander.UnitCalculation, victims, attacks);
                            if (any != null)
                            {
                                bestAttack = any;
                            }
                        }
                    }

                    if (bestAttack != null)
                    {
                        CameraManager.SetCamera(bestAttack.ToVector2(), commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.EFFECT_FUNGALGROWTH, bestAttack);
                        lastFungalFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (NeuralAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("neural");
                return true;
            }
            if (FungalAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("fungal");
                return true;
            }
            if (ShroudAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action))
            {
                TagService.TagAbility("shroud");
                return true;
            }

            return false;
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
        {
            float splashRadius = 2.25f;
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

            if (best.Value < 3 && unitCalculation.NearbyAllies.Take(25).Any(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && u.Unit.UnitType != (uint)UnitTypes.ZERG_INFESTOR) && unitCalculation.Unit.Health > 20) // only attack if going to kill 3+ units or no army to help defend
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
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

        public override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;

            if (commander.UnitCalculation.IsOnCreep)
            {
                speed *= 1.3f;
            }
            return speed;
        }
    }
}
