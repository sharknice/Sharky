namespace Sharky.MicroControllers.Zerg
{
    public class ZerglingMicroController : IndividualMicroController
    {
        public ZerglingMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health < 6 && bestTarget == null)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.ZERGLINGMOVEMENTSPEED))
            {
                if (commander.UnitCalculation.IsOnCreep)
                {
                    return 8.55f;
                }
                return 6.58f;
            }

            if (commander.UnitCalculation.IsOnCreep)
            {
                return 5.37f;
            }

            return base.GetMovementSpeed(commander);
        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.ZERGLINGATTACKSPEED))
            {
                return SharkyOptions.FramesPerSecond * 0.35f;
            }

            return base.GetWeaponCooldown(commander, enemy);
        }

        public override List<SC2Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true)
        {
            if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                return NavigateToPoint(commander, target, defensivePoint, null, frame);
            }

            return base.Scout(commander, target, defensivePoint, frame, prioritizeVision);
        }

        protected override bool AttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (enemyAttack.Unit.IsHallucination) { return false; }
            return base.AttackersFilter(commander, enemyAttack);
        }

        protected override bool GroundAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (enemyAttack.Unit.IsHallucination) { return false; }
            return base.GroundAttackersFilter(commander, enemyAttack);
        }
    }
}
