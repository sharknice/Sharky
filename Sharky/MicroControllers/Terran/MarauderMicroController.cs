namespace Sharky.MicroControllers.Terran
{
    public class MarauderMicroController : StimableMicroController
    {
        public MarauderMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (Stiming(commander))
            {
                return SharkyOptions.FramesPerSecond * 0.71f;
            }

            return base.GetWeaponCooldown(commander, enemy);       
        }
    }
}
