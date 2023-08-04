namespace Sharky.MicroControllers.Terran
{
    public class VikingMicroController : IndividualMicroController
    {
        public VikingMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count() > 0 &&
                !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.IsFlying) && 
                !commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround && e.UnitClassifications.Any(c => c == UnitClassification.ArmyUnit || c == UnitClassification.DefensiveStructure) || MapDataService.MapHeight(e.Unit.Pos) != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos)))
            {
                TagService.TagAbility("viking_land");
                action = commander.Order(frame, Abilities.MORPH_VIKINGASSAULTMODE);
                return true;
            }
            
            return false;
        }

        protected override Point2D GetSupportSpot(UnitCommander commander, UnitCalculation unitToSupport, Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(unitToSupport.Position.Y - target.Y, target.X - unitToSupport.Position.X);
            var nearestEnemy = unitToSupport.EnemiesInRange.FirstOrDefault();
            if (nearestEnemy != null)
            {
                angle = Math.Atan2(unitToSupport.Position.Y - nearestEnemy.Position.Y, nearestEnemy.Position.X - unitToSupport.Position.X);
            }
            var x = 10f * Math.Cos(angle);
            var y = 10f * Math.Sin(angle);

            var supportPoint = new Point2D { X = unitToSupport.Position.X + (float)x, Y = unitToSupport.Position.Y - (float)y };

            return supportPoint;
        }
    }
}
