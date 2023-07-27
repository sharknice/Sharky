namespace Sharky.MicroControllers.Zerg
{
    public class BroodlordMicroController : IndividualMicroController
    {
        public BroodlordMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 10 || commander.UnitCalculation.Unit.WeaponCooldown > 30; // has multiple attacks, so we do this because after one attack the cooldown starts over instead of both
        }
    }
}
