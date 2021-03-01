using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers
{
    class WorkerScoutMicroController : IndividualMicroController
    {
        public WorkerScoutMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
        {
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var enemyWorkers = commander.UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));
            if (enemyWorkers.Count() > 0)
            {
                // if any are building something
                var buildings = commander.UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(Attribute.Structure) && u.Unit.BuildProgress < 1);
                var builders = enemyWorkers.Where(w => buildings.Any(b => Vector2.DistanceSquared(w.Position, b.Position) <= b.Unit.Radius + w.Unit.Radius)).OrderBy(w => w.Unit.Health);

                if (builders.Count() > 0) // TODO: if barracks almost finished build a pylon where the addon would go LUL
                {
                    return commander.Order(frame, Abilities.ATTACK, null, builders.First().Unit.Tag);
                }

                var enemyWorker = enemyWorkers.OrderBy(e => e.Unit.Health).First();
                return commander.Order(frame, Abilities.ATTACK, null, enemyWorker.Unit.Tag);
            }
            var enemyBuildings = commander.UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(Attribute.Structure)).OrderBy(b => b.Unit.Health);
            if (enemyBuildings.Count() > 0)
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
