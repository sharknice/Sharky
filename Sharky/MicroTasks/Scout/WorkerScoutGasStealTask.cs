using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class WorkerScoutGasStealTask : MicroTask
    {
        public bool BlockExpansion { get; set; }

        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MacroData MacroData;
        MapDataService MapDataService;
        DebugService DebugService;
        BaseData BaseData;
        AreaService AreaService;

        List<Point2D> ScoutPoints;

        bool started { get; set; }

        public WorkerScoutGasStealTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, MacroData macroData, MapDataService mapDataService, bool enabled, float priority, DebugService debugService, BaseData baseData, AreaService areaService)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            MacroData = macroData;
            MapDataService = mapDataService;
            Priority = priority;
            DebugService = debugService;
            BaseData = baseData;
            AreaService = areaService;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            BlockExpansion = false;
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
                ScoutPoints = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);
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
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR) || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON))
                {
                    return commands;
                }

                if (MacroData.Minerals >= 75 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    foreach (var enemyBase in BaseData.EnemyBases)
                    {
                        foreach (var gas in enemyBase.VespeneGeysers.Where(g => g.Alliance == Alliance.Neutral))
                        {
                            if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), commander.UnitCalculation.Position) < 225)
                            {
                                var gasSteal = commander.Order(frame, Abilities.BUILD_ASSIMILATOR, null, gas.Tag);
                                if (gasSteal != null)
                                {
                                    commands.AddRange(gasSteal);
                                    return commands;
                                }
                            }
                        }
                    }
                }

                if (MacroData.Minerals >= 100 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    var expansion = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                    if (BlockExpansion && expansion != null)
                    {
                        var expansionVector = new Vector2(expansion.Location.X, expansion.Location.Y);
                        if (Vector2.DistanceSquared(expansionVector, commander.UnitCalculation.Position) < 225)
                        {
                            if (!commander.UnitCalculation.NearbyAllies.Any(a => Vector2.DistanceSquared(expansionVector, a.Position) < 9))
                            {
                                var expansionBlock = commander.Order(frame, Abilities.BUILD_PYLON, expansion.Location);
                                if (expansionBlock != null)
                                {
                                    commands.AddRange(expansionBlock);
                                    return commands;
                                }
                            }
                        }
                    }
                    foreach (var enemyBase in BaseData.EnemyBases)
                    {
                        foreach (var gas in enemyBase.VespeneGeysers.Where(g => g.Alliance == Alliance.Neutral))
                        {
                            if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), commander.UnitCalculation.Position) < 400)
                            {
                                var gasSteal = commander.Order(frame, Abilities.BUILD_ASSIMILATOR, null, gas.Tag);
                                if (gasSteal != null)
                                {
                                    commands.AddRange(gasSteal);
                                    return commands;
                                }
                            }
                        }
                    }
                }

                var action = commander.Order(frame, Abilities.MOVE, points.FirstOrDefault());
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            return commands;
        }
    }
}
