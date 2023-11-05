namespace Sharky.MicroControllers.Protoss
{
    public class ZealotMicroController : IndividualMicroController
    {
        public ZealotMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            GroupUpDistance = 5;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRangeOf.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.Worker) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
            {
                return AttackBestTargetInRange(commander, target, bestTarget, frame, out action);
            }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK) && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 170))
            {
                commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                return AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action);
            }

            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() && commander.UnitCalculation.EnemiesInRangeOfAvoid.All(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield > 0)
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() && commander.UnitCalculation.EnemiesInRangeOfAvoid.All(e => e.Range > 2))
            {
                return false;
            }

            return base.AvoidDamage(commander, target, defensivePoint, frame, out action);
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 5 || commander.UnitCalculation.Unit.WeaponCooldown > 15; // a zealot has 2 attacks, so we do this because after one attack the cooldown starts over instead of both
        }

        public override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var bestTarget = base.GetBestTarget(commander, target, frame);
            if (bestTarget == null) { return bestTarget; }

            if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag)) { return bestTarget; }

            if (bestTarget.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER)
            {
                if (commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit) && Vector2.DistanceSquared(a.Position, bestTarget.Position) < Vector2.DistanceSquared(commander.UnitCalculation.Position, bestTarget.Position)))
                {
                    return null;
                }
            }

            return bestTarget;
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.CHARGE))
            {
                return 4.725f;
            }
            return base.GetMovementSpeed(commander);
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var bestTarget = GetBestHarassTarget(commander, target);

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return MoveToTarget(commander, target, frame);
        }
    }
}
