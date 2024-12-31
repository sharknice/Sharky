namespace Sharky.MicroControllers.Terran
{
    public class HellionMicroController : IndividualMicroController
    {
        CollisionCalculator CollisionCalculator;
        UnitCountService UnitCountService;

        public HellionMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
            UnitCountService = defaultSharkyBot.UnitCountService;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (UnitCountService.Completed(UnitTypes.TERRAN_ARMORY) == 0)
            {
                return false;
            }

            if (!commander.UnitCalculation.EnemiesInRangeOf.Any() && !commander.UnitCalculation.EnemiesInRange.Any() && commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.MORPH_HELLBAT);
                return true;
            }

            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 0.3f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets)
            {
                float totalDamage = 0;
                var attackLine = GetAttackLine(commander, enemyAttack);
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (CollisionCalculator.Collides(splashedEnemy.Position, splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                    {
                        totalDamage += GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = totalDamage;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        private LineSegment GetAttackLine(UnitCommander commander, UnitCalculation enemyAttack)
        {
            // attack extends 1.65 past enemy

            var start = GetPositionFromRange(commander, enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius);
            var end = GetPositionFromRange(commander, enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range + 1.65f + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius);     

            return new LineSegment { Start = new Vector2(start.X, start.Y), End = new Vector2(end.X, end.Y) };
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            var bestTarget = GetBestHarassTarget(commander, target);

            if (WeaponReady(commander, frame) && bestTarget != null && bestTarget.UnitClassifications.HasFlag(UnitClassification.Worker) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            var workers = commander.UnitCalculation.NearbyEnemies.Where(e => e.UnitClassifications.HasFlag(UnitClassification.Worker));
            if (commander.UnitCalculation.Unit.WeaponCooldown < 15 && workers.Any())
            {
                var bestAttackPosition = GetBestAttackPosition(commander, workers, frame);                
                return commander.Order(frame, Abilities.MOVE, bestAttackPosition);
            }
            else if (workers.Any() && commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
            {
                var closestThreat = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
                if (closestThreat != null && Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) > 16 && Vector2.DistanceSquared(closestThreat.Position, target.ToVector2()) > 16)
                {
                    return commander.Order(frame, Abilities.MOVE, target);
                }
            }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return MoveToTarget(commander, target, frame);
        }

        protected LineSegment GetBestAttackLine(UnitCommander commander, IEnumerable<UnitCalculation> primaryTargets, int frame)
        {
            var weapon = UnitDataService.GetWeapon(commander.UnitCalculation.Unit);
            float splashRadius = 0.15f - commander.UnitCalculation.Unit.Radius;

            var attackPositions = new Dictionary<LineSegment, (int, float)>();
            foreach (var enemy in primaryTargets)
            {
                var startDamage = GetDamage(commander.UnitCalculation.Weapons, enemy.Unit, enemy.UnitTypeData);
                int startKills = 0;
                if (startDamage > enemy.SimulatedHitpoints)
                {
                    startKills = 1;
                }
                attackPositions[new LineSegment { Start = enemy.Position, End = enemy.Position }] = (startKills, startDamage);

                var potentialTargets = enemy.NearbyAllies.Where(e => e.FrameLastSeen == frame && Vector2.Distance(enemy.Position, e.Position) < 6.65f - enemy.Unit.Radius);
                foreach (var otherEnemy in potentialTargets)
                {
                    var start = enemy.Position;
                    var end = GetPositionFromRange(enemy.Position.X, enemy.Position.Y, otherEnemy.Position.X, otherEnemy.Position.Y, 6.65f - commander.UnitCalculation.Unit.Radius);
                    var attackLine = new LineSegment { Start = enemy.Position, End = end.ToVector2() };

                    var direction = Vector2.Normalize(attackLine.Start - attackLine.End);
                    attackLine.Start += direction * 1;

                    var damage = startDamage;
                    var kills = startKills;

                    foreach (var splashedEnemy in potentialTargets)
                    {
                        if (CollisionCalculator.Collides(splashedEnemy.Position, splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                        {
                            var currentDamage = GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                            damage += currentDamage;
                            if (currentDamage > splashedEnemy.SimulatedHitpoints)
                            {
                                kills++;
                            }
                        }
                    }

                    attackPositions[attackLine] = (kills, damage);
                }
            }

            var best = attackPositions.OrderByDescending(x => x.Value.Item1).ThenByDescending(x => x.Value.Item2).ThenBy(x => Vector2.DistanceSquared(x.Key.Start, commander.UnitCalculation.Position)).FirstOrDefault();
            DebugService.DrawLine(best.Key.Start.ToPoint(commander.UnitCalculation.Unit.Pos.Z), best.Key.End.ToPoint(commander.UnitCalculation.Unit.Pos.Z), new Color { R = 250, B = 165, G = 1 });
            return best.Key;
        }

        protected Point2D GetBestAttackPosition(UnitCommander commander, IEnumerable<UnitCalculation> primaryTargets, int frame)
        {
            var best = GetBestAttackLine(commander, primaryTargets, frame);
            return best.Start.ToPoint2D();
        }

        public override bool MoveToAttackOnCooldown(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (bestTarget != null)
            {
                if (bestTarget.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION)
                {
                    action = null;
                    return false;
                }
            }
            var targets = commander.UnitCalculation.NearbyEnemies.Where(e => DamageService.CanDamage(commander.UnitCalculation, e));
            if (targets.Any())
            {
                var bestLine = GetBestAttackLine(commander, targets, frame);
                var bestSpot = bestLine.Start.ToPoint2D();

                if (MapDataService.EnemyGroundDpsInRange(bestSpot) == 0 && MapDataService.PathWalkable(bestSpot))
                {
                    action = commander.Order(frame, Abilities.MOVE, bestSpot);
                    return true;
                }

                var direction = Vector2.Normalize(bestLine.Start - bestLine.End);

                var danger = MapDataService.EnemyGroundDpsInRange(commander.UnitCalculation.Position.ToPoint2D());
                var saferStart = bestLine.Start;
                if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
                {
                    var extension = 1f;
                    while (extension < 12f)
                    {
                        saferStart = bestLine.Start + (direction * extension);
                        var spotDanger = MapDataService.EnemyGroundDpsInRange(saferStart.ToPoint2D());
                        if (spotDanger == 0 && MapDataService.PathWalkable(saferStart))
                        {
                            action = commander.Order(frame, Abilities.MOVE, saferStart.ToPoint2D());
                            return true;
                        }
                        extension++;
                    }
                }
            }

            if (AttackIfCooldownDistanceClose(commander, bestTarget, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }
    }
}
