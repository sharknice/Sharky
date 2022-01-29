using SC2APIProtocol;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Harass
{
    public class BansheeHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        IIndividualMicroController IndividualMicroController;

        int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }

        public BansheeHarassTask(BaseData baseData, TargetingData targetingData, MapDataService mapDataService, IIndividualMicroController microController, int desiredCount = 2, bool enabled = true, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            MapDataService = mapDataService;
            IndividualMicroController = microController;
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
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
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
                    if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) && !MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos) && commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) && !MapDataService.InEnemyDetection(e.Unit.Pos)))
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else if (commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) && (commander.UnitCalculation.Unit.Health >= commander.UnitCalculation.Unit.HealthMax || (!commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() && !commander.UnitCalculation.EnemiesThreateningDamage.Any()))))
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else if (commander.UnitRole == UnitRole.Repair)
                    {
                        var reparier = commander.UnitCalculation.NearbyAllies.Where(a => (a.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV || a.Unit.UnitType == (uint)UnitTypes.TERRAN_MULE));
                        if (false && reparier.Count() > 0)
                        {
                            var action = IndividualMicroController.NavigateToPoint(commander, new Point2D { X = reparier.FirstOrDefault().Unit.Pos.X, Y = reparier.FirstOrDefault().Unit.Pos.Y }, TargetingData.ForwardDefensePoint, null, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                        else
                        {
                            var action = IndividualMicroController.Retreat(commander, TargetingData.MainDefensePoint, null, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                        if (commander.UnitCalculation.Unit.Health >= commander.UnitCalculation.Unit.HealthMax)
                        {
                            commander.UnitRole = UnitRole.Harass;
                        }
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 100)
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            harassInfo.LastClearedFrame = frame;
                            harassInfo.Harassers.Remove(commander);
                            return commands;
                        }
                        else if (commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Detector) && Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100) ||
                            commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() || commander.UnitCalculation.EnemiesThreateningDamage.Any())
                        {
                            if (commander.UnitCalculation.TargetPriorityCalculation.AirWinnability < 1)
                            {
                                harassInfo.LastDefendedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                    }
                    else if (!MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos) && commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => !e.Unit.IsFlying && (e.Unit.Health + e.Unit.Shield < commander.UnitCalculation.Damage) && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 8))  // if undetected and near one hit kills just kill them
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    //else if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
                    //{
                    //    commander.UnitRole = UnitRole.Repair;
                    //}
                    else
                    {
                        var action = IndividualMicroController.NavigateToPoint(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (commander.RetreatPath.Count() == 0)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Detector) && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 120))
                            {
                                harassInfo.LastPathFailedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }

        void AssignHarassers()
        {
            if (HarassInfos == null)
            {
                HarassInfos = new List<HarassInfo>();
                foreach (var baseLocation in BaseData.EnemyBaseLocations.Take(DesiredCount + 1))
                {
                    HarassInfos.Add(new HarassInfo { BaseLocation = baseLocation, Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1 });
                }
            }
            else
            {
                foreach (var baseLocation in BaseData.SelfBases)
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
