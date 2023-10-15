namespace Sharky.MicroControllers.Protoss
{
    public class DisruptorPhasedMicroController : IndividualMicroController
    {
        float PurificationNovaSpeed = 5.95f;
        IPathFinder NovaPathFinder;

        public DisruptorPhasedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled, IPathFinder novaPathFinder)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            NovaPathFinder = novaPathFinder;
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (PurificationNova(commander, frame, target, out action)) { return action; }

            return null;
        }

        private bool PurificationNova(UnitCommander commander, int frame, Point2D target, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            var range = ((PurificationNovaSpeed / SharkyOptions.FramesPerSecond) * commander.UnitCalculation.Unit.BuffDurationRemain) + 3f + 1f + commander.UnitCalculation.Unit.Radius;
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (!enemyAttack.Unit.IsFlying && InRange(enemyAttack.Position, commander.UnitCalculation.Position, range)) // TODO: may not need to do this on the nova itself since it's proven it can hit something already, but would be good to if performance allows it
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.OrderBy(a => GetPurificationNovaDamage(a.Unit, SharkyUnitData.UnitData[(UnitTypes)a.Unit.UnitType])).ThenByDescending(u => u.Dps);
                if (oneShotKills.Any())
                {
                    var bestAttack = GetBestAttack(commander.UnitCalculation, oneShotKills, attacks, range, frame);
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                    {
                        var airAttackers = oneShotKills.Where(u => u.DamageAir);
                        if (airAttackers.Any())
                        {
                            var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks, range, frame);
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
                            var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks, range, frame);
                            if (ground != null)
                            {
                                bestAttack = ground;
                            }
                        }
                    }

                    if (bestAttack != null)
                    {
                        CameraManager.SetCamera(bestAttack.ToVector2(), commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.MOVE, bestAttack);
                        return true;
                    }
                }
            }

            if (AvoidFriendlyFire(commander, frame, out action))
            {
                return true;
            }

            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        private bool AvoidFriendlyFire(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var closest = commander.UnitCalculation.NearbyAllies.Where(a => !a.Unit.IsFlying && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closest != null)
            {
                if (Vector2.DistanceSquared(closest.Position, commander.UnitCalculation.Position) < 25)
                {
                    var avoidPoint = GetPositionFromRange(commander, closest.Unit.Pos, commander.UnitCalculation.Unit.Pos, 3f + commander.UnitCalculation.Unit.Radius + closest.Unit.Radius + .5f);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }
            return false;
        }

        private float GetPurificationNovaDamage(Unit unit, UnitTypeData unitTypeData)
        {
            float bonusDamage = 0;
            if (unit.Shield > 0)
            {
                bonusDamage = 55;
            }

            return 145 + bonusDamage - unitTypeData.Armor; // TODO: armor upgrades
        }

        private Point2D GetBestAttack(UnitCalculation potentialAttack, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies, float range, int frame)
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
                        if (splashedEnemy.Unit.Health + splashedEnemy.Unit.Shield < GetPurificationNovaDamage(splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]))
                        {
                            killCount++;
                        }
                    }
                }
                foreach (var splashedAlly in potentialAttack.NearbyAllies.Where(a => !a.Unit.IsFlying && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTORPHASED))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + splashRadius) * (splashedAlly.Unit.Radius + splashRadius))
                    {
                        if (splashedAlly.Unit.Health + splashedAlly.Unit.Shield < GetPurificationNovaDamage(splashedAlly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedAlly.Unit.UnitType]))
                        {
                            killCount--;
                        }
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < 0) // don't kill own units
            {
                return null;
            }

            var path = NovaPathFinder.GetGroundPath(potentialAttack.Position.X, potentialAttack.Position.Y, best.Key.X, best.Key.Y, frame, range);
            if (path == null || path.Count > range)
            {
                return null;
            }

            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        public override List<SC2Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }

        public override List<SC2Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, groupCenter, frame);
        }

        public override List<SC2Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            return Attack(commander, defensivePoint, defensivePoint, null, frame);
        }

        public override List<SC2Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }

        public override List<SC2Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, null, frame);
        }
    }
}
