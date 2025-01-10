namespace Sharky.MicroControllers.Protoss
{
    public class ArchonMicroController : IndividualMicroController
    {
        public ArchonMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.IsOnScreen)
            {
                var breakpoint = true;
            }

            List<SC2Action> action = null;
            if (commander.UnitCalculation.Loaded || commander.UnitCalculation.Unit.BuildProgress < 1) { return action; }

            if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Damage > 0) || commander.UnitCalculation.EnemiesInRangeOf.Any())
            {
                return commander.Order(frame, Abilities.ATTACK, target);
            }

            return base.Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        public override bool ContinueInRangeAttack(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null && commander.UnitCalculation.EnemiesInRange.Any())
            {
                if (bestTarget.Unit.IsHallucination && commander.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsHallucination))
                {
                    return false;
                }

                action = commander.Order(frame, Abilities.ATTACK, bestTarget.Position.ToPoint2D());
                return true;
            }

            return false;
        }

        protected override bool AvoidTargetedDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOf.Any(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidTargetedDamage(commander, target, bestTarget, defensivePoint, frame, out action);
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOf.Any(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidDamage(commander, target, bestTarget, defensivePoint, frame, out action);
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return true;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 1f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets)
            {
                float dpsReduction = 0;
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        var dps = GetDps(splashedEnemy);
                        if (dps > 0)
                        {
                            dpsReduction += dps / TimeToKill(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                        }
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = dpsReduction;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }
    }
}
