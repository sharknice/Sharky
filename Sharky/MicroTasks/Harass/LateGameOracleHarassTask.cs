using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroControllers.Protoss;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Harass
{
    public class LateGameOracleHarassTask : MicroTask
    {
        IBaseManager BaseManager;
        ITargetingManager TargetingManager;
        MapDataService MapDataService;
        OracleMicroController OracleMicroController;

        public int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }

        public LateGameOracleHarassTask(IBaseManager baseManager, ITargetingManager targetingManager, MapDataService mapDataService, OracleMicroController oracleMicroController, int desiredCount = 2, bool enabled = true, float priority = -1f)
        {
            BaseManager = baseManager;
            TargetingManager = targetingManager;
            MapDataService = mapDataService;
            OracleMicroController = oracleMicroController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    if (UnitCommanders.Count() == DesiredCount)
                    {
                        return;
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            AssignHarassers();

            foreach (var harassInfo in HarassInfos)
            {
                foreach (var commander in harassInfo.Harassers)
                {
                    if (MapDataService.EnemyAirDpsInRange(commander.UnitCalculation.Unit.Pos) < 10 && ((commander.UnitCalculation.Unit.Shield >= (commander.UnitCalculation.Unit.ShieldMax / 2.0f) && commander.UnitCalculation.Unit.Energy > 50) || (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON) && commander.UnitCalculation.Unit.Shield > 5)))
                    {
                        if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 100)
                        {
                            var action = OracleMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingManager.ForwardDefensePoint, frame);
                            if (action != null)
                            {
                                commands.Add(action);
                            }

                            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker) && (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y)) <= 100)) < 1 && MapDataService.SelfVisible(harassInfo.BaseLocation.MineralLineLocation))
                            {
                                harassInfo.LastClearedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                            else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir && Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y)) < 100))
                            {
                                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.Unit.Shield <= 5)
                                {
                                    harassInfo.LastDefendedFrame = frame;
                                    harassInfo.Harassers.Remove(commander);
                                    return commands;
                                }
                            }
                        }
                        else if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir) && commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker)) > 2)  // if near woerkers and nothing can attack it just kill workers
                        {
                            var action = OracleMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingManager.ForwardDefensePoint, frame);
                            if (action != null)
                            {
                                commands.Add(action);
                            }
                        }
                        else
                        {
                            var action = OracleMicroController.NavigateToPoint(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingManager.ForwardDefensePoint, null, frame);
                            if (action != null)
                            {
                                commands.Add(action);
                            }

                            if (commander.UnitCalculation.Unit.Shield <= 10)
                            {
                                harassInfo.LastPathFailedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                    }
                    else
                    {
                        var action = OracleMicroController.NavigateToPoint(commander, TargetingManager.ForwardDefensePoint, TargetingManager.MainDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.Add(action);
                        }
                    }
                }
            }

            return commands;
        }

        bool CanHarass(UnitCommander commander, Point2D target)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }
            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker) && (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y)) <= 100)) < 1 && MapDataService.SelfVisible(new Point2D { X = target.X, Y = target.Y, }))
            {
                return false;
            }
            if (commander.UnitCalculation.Unit.Shield <= 5)
            {
                return false;
            }
            return true;
        }

        void AssignHarassers()
        {
            if (HarassInfos == null)
            {
                HarassInfos = new List<HarassInfo>();
                foreach (var baseLocation in BaseManager.BaseLocations.Where(b => b.ResourceCenter == null || b.ResourceCenter.Alliance != SC2APIProtocol.Alliance.Self).Reverse())
                {
                    HarassInfos.Add(new HarassInfo { BaseLocation = baseLocation, Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1 });
                }
            }
            else
            {
                foreach (var baseLocation in BaseManager.SelfBases)
                {
                    HarassInfos.RemoveAll(h => h.BaseLocation.Location.X == baseLocation.Location.X && h.BaseLocation.Location.Y == baseLocation.Location.Y);
                }
                foreach (var harassInfo in HarassInfos)
                {
                    harassInfo.Harassers.RemoveAll(h => !UnitCommanders.Any(u => u.UnitCalculation.Unit.Tag == h.UnitCalculation.Unit.Tag));
                }
            }

            if (HarassInfos.Count() > 0)
            {
                var unasignedCommanders = UnitCommanders.Where(u => !HarassInfos.Any(info => info.Harassers.Any(h => h.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag))).ToList();
                while (unasignedCommanders.Count() > 0)
                {
                    foreach (var info in HarassInfos.OrderBy(h => h.Harassers.Count()).ThenBy(h => HighestFrame(h)))
                    {
                        var commander = unasignedCommanders.First();
                        info.Harassers.Add(commander);
                        unasignedCommanders.Remove(commander);
                        if (unasignedCommanders.Count() == 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        int HighestFrame(HarassInfo h)
        {
            var highest = h.LastClearedFrame;
            if (h.LastDefendedFrame > highest)
            {
                highest = h.LastDefendedFrame;
            }
            if (h.LastPathFailedFrame > highest)
            {
                highest = h.LastPathFailedFrame;
            }
            return highest;
        }
    }
}
