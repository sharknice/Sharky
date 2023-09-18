namespace Sharky.MicroControllers
{
    class WorkerScoutMicroController : IndividualMicroController
    {
        public WorkerScoutMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var enemyWorkers = commander.UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));
            if (enemyWorkers.Any())
            {
                // if any are building something
                var buildings = commander.UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(SC2Attribute.Structure) && u.Unit.BuildProgress < 1);
                var builders = enemyWorkers.Where(w => buildings.Any(b => Vector2.DistanceSquared(w.Position, b.Position) <= b.Unit.Radius + w.Unit.Radius)).OrderBy(w => w.Unit.Health);

                if (builders.Any())
                {
                    return commander.Order(frame, Abilities.ATTACK, null, builders.First().Unit.Tag);
                }

                var enemyWorker = enemyWorkers.OrderBy(e => e.Unit.Health).First();
                return commander.Order(frame, Abilities.ATTACK, null, enemyWorker.Unit.Tag);
            }
            var enemyBuildings = commander.UnitCalculation.NearbyEnemies.Take(25).Where(u => u.Attributes.Contains(SC2Attribute.Structure)).OrderBy(b => b.Unit.Health);
            if (enemyBuildings.Any())
            {
                var pylon = enemyBuildings.Where(b => b.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON).FirstOrDefault();
                if (pylon != null)
                {
                    return commander.Order(frame, Abilities.ATTACK, null, pylon.Unit.Tag);
                }
                return commander.Order(frame, Abilities.ATTACK, null, enemyBuildings.First().Unit.Tag);
            }

            return commander.Order(frame, Abilities.ATTACK, target);
        }
    }
}
