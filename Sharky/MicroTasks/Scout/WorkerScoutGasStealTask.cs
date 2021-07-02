using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
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
        public bool HidePylonInBase { get; set; }
        public bool BlockWall { get; set; }

        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MacroData MacroData;
        MapDataService MapDataService;
        DebugService DebugService;
        BaseData BaseData;
        MapData MapData;
        AreaService AreaService;
        BuildingService BuildingService;
        ActiveUnitData ActiveUnitData;

        List<Point2D> ScoutPoints;
        List<Point2D> EnemyMainArea;

        bool started { get; set; }

        public WorkerScoutGasStealTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, MacroData macroData, MapDataService mapDataService, 
            bool enabled, float priority, DebugService debugService, BaseData baseData, AreaService areaService, MapData mapData, BuildingService buildingService, ActiveUnitData activeUnitData)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            MacroData = macroData;
            MapDataService = mapDataService;
            Priority = priority;
            DebugService = debugService;
            BaseData = baseData;
            AreaService = areaService;
            BuildingService = buildingService;
            MapData = mapData;
            ActiveUnitData = activeUnitData;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            BlockExpansion = false;
            HidePylonInBase = false;
            BlockWall = false;
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

            var positions = ActiveUnitData.Commanders.Values.Where(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON).Select(p => p.UnitCalculation.Position);

            if (ScoutPoints == null)
            {
                EnemyMainArea = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);
                ScoutPoints = new List<Point2D>();
                ScoutPoints.AddRange(EnemyMainArea);
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
                    if (!BaseData.EnemyBases.Any(enemyBase => enemyBase.VespeneGeysers.Any(g => g.Alliance == Alliance.Enemy)))
                    {
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
                }

                if (MacroData.Minerals >= 100 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    var enemyBase = BaseData.EnemyBaseLocations.FirstOrDefault();
                    if (BlockWall && enemyBase != null && MapData.BlockWallData != null)
                    {
                        var wallData = MapData.BlockWallData.FirstOrDefault(b => b.BasePosition.X == enemyBase.Location.X && b.BasePosition.Y == enemyBase.Location.Y);
                        if (wallData != null)
                        {
                            var vector = new Vector2(enemyBase.Location.X, enemyBase.Location.Y);
                            if (Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 225)
                            {
                                if (!ActiveUnitData.SelfUnits.Any(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Pylons.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), a.Value.Position) < 4)))
                                {
                                    foreach (var point in wallData.Pylons)
                                    {
                                        if (!BuildingService.Blocked(point.X, point.Y, 1, -.5f))
                                        {
                                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                                            if (wallBlock != null)
                                            {
                                                commands.AddRange(wallBlock);
                                                return commands;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

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

                    if (HidePylonInBase)
                    {
                        var hideLocation = EnemyMainArea.Where(p => MapDataService.SelfVisible(p) && !MapDataService.InEnemyVision(p)).OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), mainVector)).FirstOrDefault();
                        if (hideLocation != null)
                        {
                            var hidenPylonOrder = commander.Order(frame, Abilities.BUILD_PYLON, hideLocation);
                            if (hidenPylonOrder != null)
                            {
                                commands.AddRange(hidenPylonOrder);
                                return commands;
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
