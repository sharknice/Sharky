namespace Sharky.MicroControllers.Zerg
{
    public class RavagerMicroController : IndividualMicroController
    {
        private int lastBileFrame = 0;

        /// <summary>
        /// Primary target focus is to expensive units that are slow or not movable (like siege tank)
        /// Then we focus on movable expensive units or defensive structures and other units
        /// </summary>
        private Dictionary<UnitTypes, int> BileTargetScores = new()
            {
                { UnitTypes.PROTOSS_SHIELDBATTERY, 21 },
                { UnitTypes.TERRAN_LIBERATORAG, 21 },
                { UnitTypes.ZERG_LURKERMPBURROWED, 21 },

                { UnitTypes.PROTOSS_DISRUPTOR, 13 },
                { UnitTypes.PROTOSS_TEMPEST, 13 },
                { UnitTypes.PROTOSS_CARRIER, 13 },
                { UnitTypes.PROTOSS_MOTHERSHIP, 13 },
                { UnitTypes.PROTOSS_PHOTONCANNON, 13 },
                { UnitTypes.TERRAN_SIEGETANKSIEGED, 13 },
                { UnitTypes.TERRAN_WIDOWMINEBURROWED, 13 },
                { UnitTypes.TERRAN_PLANETARYFORTRESS, 13 },
                { UnitTypes.TERRAN_BUNKER, 13 },
                { UnitTypes.TERRAN_LIBERATOR, 13 },
                { UnitTypes.ZERG_LURKERMP, 13 },
                { UnitTypes.ZERG_NYDUSCANAL, 13 },
                { UnitTypes.ZERG_NYDUSNETWORK, 13 },
                { UnitTypes.ZERG_BROODLORDCOCOON, 13 },
                { UnitTypes.ZERG_BROODLORD, 13 },
                { UnitTypes.ZERG_SPINECRAWLER, 13 },

                { UnitTypes.PROTOSS_COLOSSUS, 5 },
                { UnitTypes.PROTOSS_HIGHTEMPLAR, 5 },
                { UnitTypes.PROTOSS_IMMORTAL, 5 },
                { UnitTypes.TERRAN_THOR, 5 },
                { UnitTypes.TERRAN_THORAP, 5 },
                { UnitTypes.TERRAN_SIEGETANK, 5 },
                { UnitTypes.TERRAN_GHOST, 5 },
                { UnitTypes.TERRAN_CYCLONE, 5 },
                { UnitTypes.TERRAN_MISSILETURRET, 5 },
                { UnitTypes.ZERG_ULTRALISKBURROWED, 5 },
                { UnitTypes.ZERG_RAVAGERBURROWED, 5 },
                { UnitTypes.ZERG_RAVAGERCOCOON, 5 },
                { UnitTypes.ZERG_LURKERMPEGG, 5 },
                { UnitTypes.ZERG_ULTRALISK, 5 },
                { UnitTypes.ZERG_QUEEN, 5 },
                { UnitTypes.ZERG_GREATERSPIRE, 5 },
                { UnitTypes.ZERG_VIPER, 5 },
                { UnitTypes.ZERG_LURKERDENMP, 5 },
                

                { UnitTypes.ZERG_SPORECRAWLER, 3 },
                { UnitTypes.ZERG_INFESTORBURROWED, 3 },
                { UnitTypes.ZERG_INFESTOR, 3 },
                { UnitTypes.ZERG_LARVA, 3 },// they are easier to hit
                { UnitTypes.ZERG_EGG, 3 }, // they are easier to hit
                { UnitTypes.ZERG_SPIRE, 3 },


                { UnitTypes.ZERG_HYDRALISKDEN, 2 },
                { UnitTypes.ZERG_ROACHWARREN, 2 },
            };

        public RavagerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        private int GetKillScoreForType(UnitTypes unitType)
        {
            if (BileTargetScores.TryGetValue(unitType, out var value))
                return value;
            else
                return 1;

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_CORROSIVEBILE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (lastBileFrame >= frame - 5)
            {
                return false;
            }

            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                // Exclude some units from biling completely
                if (enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_ZERGLING &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZERGLING &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZERGLINGWINGS &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_MULE &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_NUKE &&
                    enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_PARASITICBOMBDUMMY &&
                    !enemyAttack.Unit.IsHallucination &&
                    InRange(enemyAttack.Position, commander.UnitCalculation.Position, 9 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && MapDataService.SelfVisible(enemyAttack.Unit.Pos))
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
                        TagService.TagAbility("bile");
                        action = commander.Order(frame, Abilities.EFFECT_CORROSIVEBILE, bestAttack);
                        lastBileFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
        {
            float splashRadius = 0.5f;
            var killScores = new Dictionary<Point, int>();
            foreach (var enemyAttack in enemies)
            {
                int killScore = GetKillScoreForType((UnitTypes)enemyAttack.Unit.UnitType);
                foreach (var splashedEnemy in splashableEnemies)
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        killScore += GetKillScoreForType((UnitTypes)splashedEnemy.Unit.UnitType);
                    }
                }
                killScores[enemyAttack.Unit.Pos] = killScore;
            }

            var best = killScores.OrderByDescending(x => x.Value).FirstOrDefault();

            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        public override List<SC2Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
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
