namespace Sharky.MicroControllers.Protoss
{
    public class DisruptorMicroController : IndividualMicroController
    {
        int PurificationNovaRange = 13;
        private int lastPurificationFrame = 0;
        protected IPathFinder NovaPathFinder;

        public DisruptorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled, IPathFinder novaPathFinder)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            NovaPathFinder = novaPathFinder;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) 
            { 
                return true; 
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_PURIFICATIONNOVA, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (!commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < PurificationNovaRange * PurificationNovaRange))
                {
                    return false;
                }

                if (lastPurificationFrame >= frame - 5)
                {
                    return false;
                }
            }

            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying && !e.Unit.BuffIds.Contains((uint)Buffs.ORACLESTASISTRAPTARGET)))
            {
                if (enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && 
                    InRange(enemyAttack.Position, commander.UnitCalculation.Position, PurificationNovaRange + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius)) // TODO: do actual pathing to see if the shot can make it there, if a wall is in the way it can't
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.OrderByDescending(a => GetPurificationNovaDamage(a.Unit, SharkyUnitData.UnitData[(UnitTypes)a.Unit.UnitType])).ThenByDescending(u => u.Dps);
                if (oneShotKills.Any())
                {
                    var bestAttack = GetBestAttack(commander.UnitCalculation, oneShotKills, attacks, frame);
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                    {
                        var airAttackers = oneShotKills.Where(u => u.DamageAir);
                        if (airAttackers.Any())
                        {
                            var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks, frame);
                            if (air != null)
                            {
                                bestAttack = air;
                            }
                        }
                    }
                    else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                    {
                        var groundAttackers = oneShotKills.Where(u => u.DamageGround);
                        if (groundAttackers.Any())
                        {
                            var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks, frame);
                            if (ground != null)
                            {
                                bestAttack = ground;
                            }
                        }
                    }
                    else
                    {
                        if (oneShotKills.Any())
                        {
                            var any = GetBestAttack(commander.UnitCalculation, oneShotKills, attacks, frame);
                            if (any != null)
                            {
                                bestAttack = any;
                            }
                        }
                    }

                    if (bestAttack != null)
                    {
                        TagService.TagAbility("purification");
                        CameraManager.SetCamera(bestAttack.ToVector2(), commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.EFFECT_PURIFICATIONNOVA, bestAttack);
                        lastPurificationFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        protected float GetPurificationNovaDamage(Unit unit, UnitTypeData unitTypeData)
        {
            float bonusDamage = 0;
            if (unit.Shield > 0)
            {
                bonusDamage = 55;
            }

            return 145 + bonusDamage - unitTypeData.Armor; // TODO: armor upgrades
        }

        protected Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies, int frame)
        {
            float splashRadius = 1.5f;
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
                foreach (var splashedAlly in unitCalculation.NearbyAllies.Take(25).Where(a => !a.Unit.IsFlying && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + splashRadius) * (splashedAlly.Unit.Radius + splashRadius))
                    {
                        killCount--;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            if (killCounts.Count() == 0)
            {
                return null;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            // only go for 3+ unit clumps unless against queens, thors, or tanks, or if no other friendly army to kill enemy
            if (!unitCalculation.EnemiesThreateningDamage.Any() &&
                best.Value < 3 && 
                unitCalculation.NearbyAllies.Take(25).Any(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && u.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR) && 
                !enemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || e.Unit.UnitType == (uint)UnitTypes.TERRAN_THOR || e.Unit.UnitType == (uint)UnitTypes.TERRAN_THORAP || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
            {
                return null;
            }

            var path = NovaPathFinder.GetGroundPath(unitCalculation.Position.X, unitCalculation.Position.Y, best.Key.X, best.Key.Y, frame, PurificationNovaRange);
            if (path == null || path.Count > PurificationNovaRange)
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

        protected override bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive || MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 1)
            {
                return false;
            }

            var range = 9f;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Take(25).Where(enemyAttack => DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance));

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(commander, closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            if (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                if (MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos))
                {
                    return false;
                }
            }

            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        protected override bool ShouldStayOutOfRange(UnitCommander commander, int frame)
        {
            return MicroPriority == MicroPriority.StayOutOfRange || !commander.AbilityOffCooldown(Abilities.EFFECT_PURIFICATIONNOVA, frame, SharkyOptions.FramesPerSecond, SharkyUnitData);
        }
    }
}
