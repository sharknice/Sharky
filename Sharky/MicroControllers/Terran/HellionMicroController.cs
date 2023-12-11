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

            if (WeaponReady(commander, frame) && bestTarget != null && bestTarget.UnitClassifications.Contains(UnitClassification.Worker) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            var workers = commander.UnitCalculation.NearbyEnemies.Where(e => e.UnitClassifications.Contains(UnitClassification.Worker));
            if (commander.UnitCalculation.Unit.WeaponCooldown < 15 && workers.Any())
            {
                var bestAttackPosition = GetBestAttackPosition(commander, workers, frame);                
                return commander.Order(frame, Abilities.MOVE, bestAttackPosition);
            }
            else if (workers.Any() && commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)))
            {
                var closestThreat = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
                if (closestThreat != null && Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) > 16 && Vector2.DistanceSquared(closestThreat.Position, target.ToVector2()) > 16)
                {
                    return commander.Order(frame, Abilities.MOVE, target);
                }
            }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return MoveToTarget(commander, target, frame);
        }

        protected Point2D GetBestAttackPosition(UnitCommander commander, IEnumerable<UnitCalculation> primaryTargets, int frame)
        {
            var weapon = UnitDataService.GetWeapon(commander.UnitCalculation.Unit);
            float splashRadius = 0.15f - commander.UnitCalculation.Unit.Radius;

            var attackPositions = new Dictionary<Point2D, (int, float)>();
            foreach (var enemy in primaryTargets)
            {
                var startDamage = GetDamage(commander.UnitCalculation.Weapons, enemy.Unit, enemy.UnitTypeData);
                int startKills = 0;
                if (startDamage > enemy.SimulatedHitpoints)
                {
                    startKills = 1;
                }
                attackPositions[enemy.Unit.Pos.ToPoint2D()] = (startKills, startDamage);

                var potentialTargets = enemy.NearbyAllies.Where(e => e.FrameLastSeen == frame && Vector2.Distance(enemy.Position, e.Position) < 6.65f - enemy.Unit.Radius);
                foreach (var otherEnemy in potentialTargets)
                {
                    var start = enemy.Position;
                    var end = GetPositionFromRange(enemy.Position.X, enemy.Position.Y, otherEnemy.Position.X, otherEnemy.Position.Y, 6.65f - commander.UnitCalculation.Unit.Radius);
                    var attackLine = new LineSegment { Start = enemy.Position, End = new Vector2(end.X, end.Y) };

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

                    var attackPosition = GetPositionFromRange(end.X, end.Y, enemy.Position.X, enemy.Position.Y, 6.65f);
                    attackPositions[attackPosition] = (kills, damage);
                }

            }

            return attackPositions.OrderByDescending(x => x.Value.Item1).ThenByDescending(x => x.Value.Item2).ThenBy(x => Vector2.DistanceSquared(x.Key.ToVector2(), commander.UnitCalculation.Position)).FirstOrDefault().Key;
        }
    }
}
