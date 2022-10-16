using Sharky.DefaultBot;
using Sharky.Pathing;

namespace Sharky.MicroControllers.Terran
{
    public class MarineMicroController : StimableMicroController
    {
        public MarineMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (Stiming(commander))
            {
                return SharkyOptions.FramesPerSecond * 0.407f;
            }

            return base.GetWeaponCooldown(commander, enemy);
        }
    }
}
