using SC2APIProtocol;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class WorkerScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        IIndividualMicroController IndividualMicroController;
        DebugService DebugService;
        BaseData BaseData;

        List<Point2D> ScoutPoints;

        bool started { get; set; }

        public WorkerScoutTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, MapDataService mapDataService, bool enabled, float priority, IIndividualMicroController individualMicroController, DebugService debugService, BaseData baseData)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            MapDataService = mapDataService;
            Priority = priority;
            IndividualMicroController = individualMicroController;
            DebugService = debugService;
            BaseData = baseData;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (ScoutPoints == null)
            {
                ScoutPoints = GetScoutArea(TargetingData.EnemyMainBasePoint);
                ScoutPoints.Add(BaseData.EnemyBaseLocations.Skip(1).First().Location);
            }

            var mainVector = new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y);
            var points = ScoutPoints.OrderBy(p => MapDataService.LastFrameVisibility(p)).ThenByDescending(p => Vector2.DistanceSquared(mainVector, new Vector2(p.X, p.Y)));

            foreach (var point in points)
            {
                //DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
            }

            foreach (var commander in UnitCommanders)
            {
                var action = commander.Order(frame, Abilities.MOVE, points.FirstOrDefault());
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            return commands;
        }

        List<Point2D> GetScoutArea(Point2D point)
        {
            var points = new List<Point2D>();

            var startHeight = MapDataService.MapHeight(point);
            for (var x = -25; x < 25; x++)
            {
                for (var y = -25; y < 25; y++)
                {
                    if (x + point.X > 0 && x + point.X < MapDataService.MapData.MapWidth && y + point.Y > 0 && y + point.Y < MapDataService.MapData.MapHeight)
                    {
                        if (MapDataService.MapHeight(x + (int)point.X, y + (int)point.Y) == startHeight && MapDataService.PathWalkable(x + (int)point.X, y + (int)point.Y))
                        {
                            points.Add(new Point2D { X = x + (int)point.X, Y = y + (int)point.Y });
                        }
                    }
                }
            }

            return points;
        }
    }
}
