using SC2APIProtocol;
using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.MacroServices
{
    public class WorkerBuilderService
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;

        public WorkerBuilderService(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
        }

        public UnitCommander GetWorker(Point2D location, IEnumerable<UnitCommander> workers = null)
        {
            IEnumerable<UnitCommander> availableWorkers;
            if (workers == null)
            {
                availableWorkers = ActiveUnitData.Commanders.Values.Where(c => c.UnitRole == UnitRole.Build && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && c.UnitCalculation.Unit.Orders.Any(o => ActiveUnitData.SelfUnits.Values.Any(s => s.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && s.Unit.BuildProgress == 1 && o.TargetWorldSpacePos != null && s.Position.X == o.TargetWorldSpacePos.X && s.Position.Y == o.TargetWorldSpacePos.Y))).Concat(
                    ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.PreBuild || c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
                    .OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, new Vector2(location.X, location.Y)));
            }
            else
            {
                availableWorkers = workers.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))).OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, new Vector2(location.X, location.Y)));
            }

            if (availableWorkers.Count() == 0)
            {
                return null;
            }
            else
            {
                var closest = availableWorkers.First();
                var pos = closest.UnitCalculation.Position;
                var distanceSquared = Vector2.DistanceSquared(pos, new Vector2(location.X, location.Y));
                if (distanceSquared > 1000)
                {
                    pos = availableWorkers.First().UnitCalculation.Position;

                    if (Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(location.X, location.Y)) > distanceSquared)
                    {
                        return closest;
                    }
                    else
                    {
                        return availableWorkers.First();
                    }
                }
            }
            return availableWorkers.First();
        }
    }
}
