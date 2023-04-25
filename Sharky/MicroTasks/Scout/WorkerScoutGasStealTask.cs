using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class WorkerScoutGasStealTask : MicroTask
    {
        public bool StealGas { get; set; }
        public bool BlockExpansion { get; set; }
        public bool BlockLiftedExpansion { get; set; }
        public bool HidePylonInBase { get; set; }
        public bool BlockWall { get; set; }
        public bool BlockAddons { get; set; }
        public bool RecallProbe { get; set; }

        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MacroData MacroData;
        MapDataService MapDataService;
        DebugService DebugService;
        BaseData BaseData;
        MapData MapData;
        EnemyData EnemyData;
        AreaService AreaService;
        BuildingService BuildingService;
        UnitCountService UnitCountService;
        ActiveUnitData ActiveUnitData;
        SharkyOptions SharkyOptions;
        IBuildingBuilder BuildingBuilder;

        MineralWalker MineralWalker;

        List<Point2D> ScoutPoints;
        List<Point2D> EnemyMainArea;

        IIndividualMicroController IndividualMicroController;

        bool started { get; set; }


        public WorkerScoutGasStealTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
            AreaService = defaultSharkyBot.AreaService;
            BuildingService = defaultSharkyBot.BuildingService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            MapData = defaultSharkyBot.MapData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MineralWalker = defaultSharkyBot.MineralWalker;
            BuildingBuilder = defaultSharkyBot.BuildingBuilder;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            UnitCommanders = new List<UnitCommander>();

            Enabled = enabled;
            Priority = priority;

            StealGas = true;
            BlockExpansion = false;
            BlockLiftedExpansion = false;
            HidePylonInBase = false;
            BlockWall = false;
            BlockAddons = false;
            RecallProbe = false;
            IndividualMicroController = individualMicroController;
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

                var workers = commanders.Where(commander => commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker));
                var scouter = workers.FirstOrDefault(commander => !commander.Value.Claimed);
                if (scouter.Value == null)
                {
                    var available = workers.Where(commander => !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)));
                    var finishedBuilder = available.FirstOrDefault(commander => commander.Value.UnitRole == UnitRole.Build && commander.Value.UnitCalculation.Unit.Orders.Any(o => !o.HasTargetUnitTag));
                    if (finishedBuilder.Value != null)
                    {
                        var pos = finishedBuilder.Value.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.TargetWorldSpacePos != null);
                        if (pos != null)
                        {
                            var match = finishedBuilder.Value.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && a.Unit.Pos.X == pos.TargetWorldSpacePos.X && a.Unit.Pos.Y == pos.TargetWorldSpacePos.Y);
                            if (match)
                            {
                                finishedBuilder.Value.Claimed = true;
                                finishedBuilder.Value.UnitRole = UnitRole.Scout;
                                UnitCommanders.Add(finishedBuilder.Value);
                                started = true;
                                return;
                            }
                        }
                    }
                    if (scouter.Value == null)
                    {
                        scouter = available.FirstOrDefault();
                    }
                }

                if (scouter.Value != null)
                {
                    scouter.Value.Claimed = true;
                    scouter.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(scouter.Value);
                    started = true;
                    return;
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

            bool disable = false;

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR) || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON))
                {
                    continue;
                }

                if (commander.UnitCalculation.Unit.ShieldMax > 5 && (commander.UnitCalculation.Unit.Shield < 5 || (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.Contains(UnitClassification.Worker)) > 0)))
                {
                    if (MineralWalker.MineralWalkHome(commander, frame, out List<SC2APIProtocol.Action> mineralWalk))
                    {
                        commands.AddRange(mineralWalk);
                        continue;
                    }
                }

                if (RecallProbe)
                {
                    if (frame > 1.9 * 60 * SharkyOptions.FramesPerSecond)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) == 1 && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ProductionStructure)))
                        {
                            var nexus = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && c.UnitCalculation.Unit.Energy >= 50);
                            if (nexus != null)
                            {
                                var baseLocation = BaseData.SelfBases.FirstOrDefault(b => b.ResourceCenter != null && b.ResourceCenter.Tag == nexus.UnitCalculation.Unit.Tag);
                                if (baseLocation != null)
                                {
                                    var angle = Math.Atan2(baseLocation.Location.Y - baseLocation.MineralLineLocation.Y, baseLocation.MineralLineLocation.X - baseLocation.Location.X);
                                    var recallPoint = new Point2D { X = commander.UnitCalculation.Position.X + (float)(-2 * Math.Cos(angle)), Y = commander.UnitCalculation.Position.Y - (float)(-2 * Math.Sin(angle)) };
                                    var recall = nexus.Order(frame, Abilities.NEXUSMASSRECALL, recallPoint);
                                    if (recall != null)
                                    {
                                        commands.AddRange(recall);
                                        commands.AddRange(commander.Order(frame, Abilities.MOVE, commander.UnitCalculation.Position.ToPoint2D()));
                                        disable = true;
                                        break;
                                    }
                                }
                            }
                            if (disable) { continue; }
                        }
                    }

                    if (frame > 1.75 * 60 * SharkyOptions.FramesPerSecond)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && e.Unit.IsActive))
                        {
                            var scout = IndividualMicroController.Scout(commander, BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation, TargetingData.ForwardDefensePoint, frame, false, true);
                            if (scout != null)
                            {
                                commands.AddRange(scout);
                            }
                            continue;
                        }
                    }
                }

                if (StealGas && MacroData.Minerals >= 75 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    if (!BaseData.EnemyBases.Any(enemyBase => enemyBase.VespeneGeysers.Any(g => g.Alliance == Alliance.Enemy)) && MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) > 0)
                    {
                        foreach (var enemyBase in BaseData.EnemyBases.Where(enemyBase => enemyBase.ResourceCenter != null && enemyBase.ResourceCenter.BuildProgress == 1))
                        {
                            foreach (var gas in enemyBase.VespeneGeysers.Where(g => g.Alliance == Alliance.Neutral))
                            {
                                if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), commander.UnitCalculation.Position) < 400)
                                {
                                    var gasSteal = commander.Order(frame, Abilities.BUILD_ASSIMILATOR, null, gas.Tag);
                                    if (gasSteal != null)
                                    {
                                        commands.AddRange(gasSteal);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }

                if (MacroData.Minerals >= 100 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    var enemyBase = BaseData.EnemyBaseLocations.FirstOrDefault();

                    if (BlockAddons && EnemyData.EnemyRace == Race.Terran)
                    {
                        var buildingWithoutAddon = commander.UnitCalculation.NearbyEnemies.Where(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || e.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || e.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT) && !e.Unit.HasAddOnTag && BuildingBuilder.HasRoomForAddon(e.Unit)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (buildingWithoutAddon != null)
                        {
                            var point = new Point2D { X = buildingWithoutAddon.Unit.Pos.X + 2.5f, Y = buildingWithoutAddon.Unit.Pos.Y - .5f };
                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                            if (wallBlock != null)
                            {
                                commands.AddRange(wallBlock);
                                continue;
                            }
                        }
                    }

                    if (BlockWall && enemyBase != null && MapData.WallData != null)
                    {
                        var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == enemyBase.Location.X && b.BasePosition.Y == enemyBase.Location.Y);
                        if (wallData != null)
                        {
                            var vector = new Vector2(enemyBase.Location.X, enemyBase.Location.Y);
                            if (Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 225)
                            {
                                if (wallData.Depots != null && !ActiveUnitData.SelfUnits.Any(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Depots.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), a.Value.Position) < 4)))
                                {
                                    foreach (var point in wallData.Depots)
                                    {
                                        if (!BuildingService.Blocked(point.X, point.Y, 1, -.5f))
                                        {
                                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                                            if (wallBlock != null)
                                            {
                                                commands.AddRange(wallBlock);
                                                continue;
                                            }
                                        }
                                    }
                                }
                                if (wallData.Production != null && !ActiveUnitData.SelfUnits.Any(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Production.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), a.Value.Position) < 4)))
                                { 
                                    foreach (var point in wallData.Production)
                                    {
                                        if (!BuildingService.Blocked(point.X, point.Y, 1, -.5f))
                                        {
                                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                                            if (wallBlock != null)
                                            {
                                                commands.AddRange(wallBlock);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var expansion = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                    if (expansion != null)
                    {
                        if (BlockExpansion || (BlockLiftedExpansion && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) > 1))
                        {
                            var expansionVector = new Vector2(expansion.Location.X, expansion.Location.Y);
                            if (Vector2.DistanceSquared(expansionVector, commander.UnitCalculation.Position) < 225)
                            {
                                if (!commander.UnitCalculation.NearbyAllies.Any(a => Vector2.DistanceSquared(expansionVector, a.Position) < 9) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ResourceCenter) && !e.Unit.IsFlying && Vector2.DistanceSquared(expansionVector, e.Position) < 2))
                                {
                                    var expansionBlock = commander.Order(frame, Abilities.BUILD_PYLON, expansion.Location);
                                    if (expansionBlock != null)
                                    {
                                        commands.AddRange(expansionBlock);
                                        continue;
                                    }
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
                                continue;
                            }
                        }
                    }
                }

                if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
                {
                    var wallData = MapData.WallData.FirstOrDefault(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y);
                    if (wallData != null)
                    {
                        var production = wallData?.Production?.FirstOrDefault();
                        if (production != null)
                        {
                            var vector = new Vector2(production.X, production.Y);
                            if (Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 25)
                            {
                                if (wallData.Depots != null && wallData.Depots.All(d => commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT && e.Unit.Pos.X == d.X && e.Unit.Pos.Y == d.Y)))
                                {
                                    var prodBuilding = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !e.Unit.IsFlying && e.Unit.Pos.X == production.X && e.Unit.Pos.Y == production.Y);
                                    if (prodBuilding != null)
                                    {
                                        commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: prodBuilding.Unit.Tag));
                                        continue;
                                    }
                                    else
                                    {
                                        if (MacroData.Minerals >= 100 && Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 4 && !commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && a.Unit.Pos.X == production.X && a.Unit.Pos.Y == production.Y))
                                        {
                                            commands.AddRange(commander.Order(frame, Abilities.BUILD_PYLON, new Point2D { X = production.X, Y = production.Y }));
                                            continue;
                                        }
                                        else
                                        {
                                            commands.AddRange(commander.Order(frame, Abilities.MOVE, new Point2D { X = production.X, Y = production.Y }));
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var liftedBuilding = commander.UnitCalculation.NearbyEnemies.Where(e => e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.Unit.IsFlying).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
                    if (liftedBuilding != null)
                    {
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, new Point2D { X = liftedBuilding.Position.X, Y = liftedBuilding.Position.Y }));
                        continue;
                    }

                    // if worker building building nearby, attack it
                    if (commander.UnitCalculation.Unit.Shield > 5)
                    {
                        var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => 
                            e.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && 
                                e.NearbyAllies.Any(a => a.Unit.BuildProgress < 1 && Vector2.DistanceSquared(a.Position, e.Position) < ((e.Unit.Radius + a.Unit.Radius + 1) * (e.Unit.Radius + a.Unit.Radius + 1)))
                            );
                        if (enemy != null)
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: enemy.Unit.Tag));
                            continue;
                        }
                    }
                }

                if (commander.UnitCalculation.Unit.Shield > 15 && commander.UnitCalculation.NearbyEnemies.Any() && (commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) || (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ResourceCenter) && e.Unit.BuildProgress == 1) && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))))
                {
                    commands.AddRange(commander.Order(frame, Abilities.ATTACK, points.FirstOrDefault()));
                    continue;
                }

                var action = commander.Order(frame, Abilities.MOVE, points.FirstOrDefault());
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            if (disable)
            {
                Disable();
            }

            return commands;
        }
    }
}
