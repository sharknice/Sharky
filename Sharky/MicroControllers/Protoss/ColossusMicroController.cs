namespace Sharky.MicroControllers.Protoss
{
    public class ColossusMicroController : IndividualMicroController
    {
        CollisionCalculator CollisionCalculator;

        public ColossusMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
        }

        protected override bool DealWithSiegedTanks(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.IsHallucination)
            {
                return base.DealWithSiegedTanks(commander, target, defensivePoint, frame, out action);
            }
            if (!WeaponReady(commander, frame))
            {
                var attack = commander.UnitCalculation.EnemiesInRangeOfAvoid.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (attack != null)
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + 5);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            action = null;
            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 0.3f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets)
            {
                float totalDamage = 0;
                var attackLine = GetAttackLine(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos);
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (CollisionCalculator.Collides(splashedEnemy.Position, splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                    {
                        totalDamage +=  GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = totalDamage;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        private LineSegment GetAttackLine(Point start, Point end)
        {
            var length = 1.4f;
            var dx = start.X - end.X;
            var dy = start.Y - end.Y;
            var dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
            dx /= dist;
            dy /= dist;
            var attackStart = new Vector2(start.X + (length * dy), start.Y - (length * dx));
            var attackEnd = new Vector2(start.X - (length * dy), start.Y + (length * dx));
            return new LineSegment { Start = attackStart, End = attackEnd };
        }

        public override List<SC2APIProtocol.Action> MoveToTarget(UnitCommander commander, Point2D target, int frame)
        {
            return commander.Order(frame, Abilities.MOVE, target);
        }

        protected override List<SC2APIProtocol.Action> AttackToTarget(UnitCommander commander, Point2D target, int frame)
        {
            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public override bool FollowPath(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var point = commander.RetreatPath.LastOrDefault();
            if (point != Vector2.Zero)
            {
                action = commander.Order(frame, Abilities.MOVE, new Point2D { X = point.X, Y = point.Y });

                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, point) < 4)
                {
                    commander.RetreatPathIndex = commander.RetreatPath.Count;
                }

                return true;
            }

            return false;
        }
    }
}
