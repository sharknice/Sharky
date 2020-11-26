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

        public Action BuildBuilding(MacroData macroData, UnitTypes unitType, BuildingTypeData unitData, Point2D generalLocation = null, bool ignoreMineralProximity = false, float maxDistance = 50)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var location = generalLocation;
                if (location == null)
                {
                    location = GetReferenceLocation(TargetingManager.SelfMainBasePoint);
                }
                var placementLocation = BuildingPlacement.FindPlacement(location, unitType, unitData.Size, ignoreMineralProximity, maxDistance);
                
                if (placementLocation != null)
                {
                    var worker = GetWorker(placementLocation);
                    if (worker != null)
                    {
                        return worker.Order(macroData.Frame, unitData.Ability, placementLocation);
                    }
                }
             }

            return null;
        }

        public Action BuildAddOn(MacroData macroData, TrainingTypeData unitData)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && !c.Value.UnitCalculation.Unit.HasAddOnTag);
                if (building.Count() > 0)
                {
                    var action = building.First().Value.Order(macroData.Frame, unitData.Ability);
                    if (action != null)
                    {
                        return action;
                    }
                }
            }

            return null;
        }

        public Action BuildGas(MacroData macroData, BuildingTypeData unitData, Unit geyser)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var worker = GetWorker(new Point2D { X = geyser.Pos.X, Y = geyser.Pos.Y });
                if (worker != null)
                {
                    return worker.Order(macroData.Frame, unitData.Ability, null, geyser.Tag);
                }
            }

            return null;
        }

        public Point2D GetReferenceLocation(Point2D buildLocation)
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
