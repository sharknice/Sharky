using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Managers;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds
{
    public class BuildingBuilder
    {

        IUnitManager UnitManager;
        ITargetingManager TargetingManager;
        IBuildingPlacement BuildingPlacement;
        UnitDataManager UnitDataManager;

        public BuildingBuilder(IUnitManager unitManager, ITargetingManager targetingManager, IBuildingPlacement buildingPlacement, UnitDataManager unitDataManager)
        {
            UnitManager = unitManager;
            TargetingManager = targetingManager;
            BuildingPlacement = buildingPlacement;
            UnitDataManager = unitDataManager;
        }

        public ActionRawUnitCommand BuildBuilding(MacroManager macroManager, UnitTypes unitType, BuildingTypeData unitData)
        {
            if (unitData.Minerals <= macroManager.Minerals && unitData.Gas <= macroManager.VespeneGas)
            {
                var location = GetReferenceLocation(TargetingManager.DefensePoint);
                var placementLocation = BuildingPlacement.FindPlacement(location, unitType, unitData.Size);
                
                if (placementLocation != null)
                {
                    var worker = GetWorker(placementLocation);
                    if (worker != null)
                    {
                        return worker.Order(macroManager.Frame, unitData.Ability, placementLocation);
                    }
                }
             }

            return null;
        }

        private Point2D GetReferenceLocation(Point2D buildLocation)
        {
            var nexus = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter)).OrderBy(c => Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(buildLocation.X, buildLocation.Y))).FirstOrDefault();
            if (nexus != null)
            {
                return new Point2D { X = nexus.UnitCalculation.Unit.Pos.X, Y = nexus.UnitCalculation.Unit.Pos.Y };
            }
            else
            {
                var worker = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker)).OrderBy(c => c.UnitCalculation.NearbyEnemies.Count()).ThenBy(c => c.UnitCalculation.NearbyAllies.Where(ally => ally.UnitClassifications.Contains(UnitClassification.ArmyUnit)).Count()).FirstOrDefault();
                if (worker != null)
                {
                    return new Point2D { X = worker.UnitCalculation.Unit.Pos.X, Y = worker.UnitCalculation.Unit.Pos.Y };
                }
            }

            return buildLocation;
        }

        private UnitCommander GetWorker(Point2D location)
        {
            var workers = UnitManager.Commanders.Where(c => c.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !c.Value.UnitCalculation.Unit.BuffIds.Any(b => UnitDataManager.CarryingResourceBuffs.Contains((Buffs)b)) && !c.Value.UnitCalculation.Unit.Orders.Any(o => UnitDataManager.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId)));

            //var minerals = UnitManager.NeutralUnits.Values.Where(u => u.Unit.UnitType == UnitTypes.NEUTRAL_MINERALFIELD)
            //var probesNotMiningThisInstant = probes.Where(p => !minerals.Any(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(p.Value.Unit.Pos.X, p.Value.Unit.Pos.Y)) < 3));

            var closestWorkers = workers.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.Value.UnitCalculation.Unit.Pos.X, p.Value.UnitCalculation.Unit.Pos.Y), new Vector2(location.X, location.Y)));
            if (closestWorkers.Count() == 0)
            {
                return null;
            }
            else
            {
                var closest = closestWorkers.First().Value;
                var pos = closest.UnitCalculation.Unit.Pos;
                var distanceSquared = Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(location.X, location.Y));
                if (distanceSquared > 1000)
                {
                    closestWorkers = workers.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.Value.UnitCalculation.Unit.Pos.X, p.Value.UnitCalculation.Unit.Pos.Y), new Vector2(location.X, location.Y)));
                    pos = closestWorkers.First().Value.UnitCalculation.Unit.Pos;

                    if (Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(location.X, location.Y)) > distanceSquared)
                    {
                        return closest;
                    }
                    else
                    {
                        return closestWorkers.First().Value;
                    }
                }
            }
            return closestWorkers.First().Value;
        }
    }
}
