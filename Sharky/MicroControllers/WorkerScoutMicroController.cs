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
            var enemyWorkers = commander.UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.HasFlag(UnitClassification.Worker));
            if (enemyWorkers.Any())
            {
                // if any are building something
                var buildings = commander.UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(SC2Attribute.Structure) && u.Unit.BuildProgress < 1);
                var builders = enemyWorkers.Where(w => buildings.Any(b => Vector2.DistanceSquared(w.Position, b.Position) <= b.Unit.Radius + w.Unit.Radius)).OrderBy(w => w.Unit.Health);

                if (builders.Any())
                {
                    if (builders.First().FrameLastSeen == frame)
                    {
                        return commander.Order(frame, Abilities.ATTACK, null, builders.First().Unit.Tag);
                    }
                    else
                    {
                        return commander.Order(frame, Abilities.ATTACK, builders.First().Position.ToPoint2D());
                    }
                }

                var enemyWorker = enemyWorkers.OrderBy(e => e.Unit.Health).First();
                if (enemyWorker.FrameLastSeen == frame)
                {
                    return commander.Order(frame, Abilities.ATTACK, null, enemyWorker.Unit.Tag);
                }
                else
                {
                    return commander.Order(frame, Abilities.ATTACK, enemyWorker.Position.ToPoint2D());
                }
            }
            var enemyBuildings = commander.UnitCalculation.NearbyEnemies.Take(25).Where(u => u.Attributes.Contains(SC2Attribute.Structure)).OrderBy(b => b.Unit.Health);
            if (enemyBuildings.Any())
            {
                var pylon = enemyBuildings.Where(b => b.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON).FirstOrDefault();
                if (pylon != null)
                {
                    if (pylon.FrameLastSeen == frame)
                    {
                        return commander.Order(frame, Abilities.ATTACK, null, pylon.Unit.Tag);
                    }
                    else
                    {
                        return commander.Order(frame, Abilities.ATTACK, pylon.Position.ToPoint2D());
                    }
                }
                if (enemyBuildings.First().FrameLastSeen == frame)
                {
                    return commander.Order(frame, Abilities.ATTACK, null, enemyBuildings.First().Unit.Tag);
                }
                else
                {
                    return commander.Order(frame, Abilities.ATTACK, enemyBuildings.First().Position.ToPoint2D());
                }
            }

            return commander.Order(frame, Abilities.ATTACK, target);
        }
    }
}
