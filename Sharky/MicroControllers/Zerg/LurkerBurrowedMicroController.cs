﻿namespace Sharky.MicroControllers.Zerg
{
    public class LurkerBurrowedMicroController : IndividualMicroController
    {
        public LurkerBurrowedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            var lurkerRange = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.LURKERRANGE) ? 10 : 8;

            var groundEnemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsFlying &&
                (Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < lurkerRange * lurkerRange));

            if (groundEnemiesInRange.Count() == 0)
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.BURROWUP_LURKER);
                return true;
            }

            action = null;
            return false;
        }
    }
}
