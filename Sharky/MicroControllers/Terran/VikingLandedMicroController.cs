namespace Sharky.MicroControllers.Terran
{
    public class VikingLandedMicroController : IndividualMicroController
    {
        public VikingLandedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count() == 0 ||
                commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.IsFlying) || 
                commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround && e.UnitClassifications.Any(c => c == UnitClassification.ArmyUnit || c == UnitClassification.DefensiveStructure)))
            {
                TagService.TagAbility("viking_fly");
                action = commander.Order(frame, Abilities.MORPH_VIKINGFIGHTERMODE);
                return true;
            }
            
            return false;
        }
    }
}
