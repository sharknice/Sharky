using Sharky.DefaultBot;
using Sharky.Pathing;

namespace Sharky.MicroControllers.Terran
{
    public class ThorMicroController : IndividualMicroController
    {
        public ThorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown == 0 || commander.UnitCalculation.Unit.WeaponCooldown > 2; // a thor has multiple attacks, don't cancel the animation early
        }
    }
}
