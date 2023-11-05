namespace Sharky.MicroControllers.Zerg
{
    public class UltraliskMicroController : IndividualMicroController
    {
        public UltraliskMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.ANABOLICSYNTHESIS))
            {
                speed += 0.82f;
            }
            if (commander.UnitCalculation.IsOnCreep)
            {
                return 5.37f;
            }
            return speed;
        }
    }
}
