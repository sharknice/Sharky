namespace Sharky.MicroControllers.Terran
{
    public class ThorMicroController : IndividualMicroController
    {
        public ThorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown == 0 || commander.UnitCalculation.Unit.WeaponCooldown > 2; // a thor has multiple attacks, don't cancel the animation early
        }

        public override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (EnemyData.EnemyRace == Race.Protoss)
            {
                TagService.TagAbility("high_impact");
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.MORPH_THORHIGHIMPACTMODE);
                return true;
            }
            else if (EnemyData.EnemyRace == Race.Terran)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BATTLECRUISER))
                {
                    TagService.TagAbility("high_impact");
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.MORPH_THORHIGHIMPACTMODE);
                    return true;
                }
            }

            return false;
        }
    }
}
