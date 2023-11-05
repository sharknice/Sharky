namespace Sharky.MicroControllers.Zerg
{
    public class LurkerMicroController : IndividualMicroController
    {
        public LurkerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            var lurkerRange = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.LURKERRANGE) ? 10 : 8;

            var groundEnemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying &&
                (Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < (lurkerRange - 1.5f) * (lurkerRange - 1.5f)));

            if (groundEnemiesInRange.Any())
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.BURROWDOWN_LURKER);
                return true;
            }
            else
            {
                action = MoveToTarget(commander, target, frame);
                return true;
            }
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;

            if (SharkyUnitData.ResearchedUpgrades.Contains((int)Upgrades.LURKERSPEED))
            {
                speed += 0.413f;
            }
            if (commander.UnitCalculation.IsOnCreep)
            {
                speed *= 1.3f;
            }
            return speed;
        }
    }
}
