using Sharky.MicroControllers;
using Sharky.MicroTasks.Harass;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ReaperWorkerHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        IIndividualMicroController MicroController;

        int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }

        public ReaperWorkerHarassTask(BaseData baseData, TargetingData targetingData, IIndividualMicroController microController, int desiredCount = 2, bool enabled = false, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            MicroController = microController;
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
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER)
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
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 100)
                    {
                        var action = MicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            harassInfo.LastClearedFrame = frame;
                            harassInfo.Harassers.Remove(commander);
                            return commands;
                        }
                        else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.DamageGround && Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            if (commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability < 1 && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax)
                            {
                                harassInfo.LastDefendedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                    }
                    else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                    {
                        var action = MicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                            continue;
                        }
                    }
                    else
                    {
                        var action = MicroController.NavigateToPoint(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (commander.RetreatPath.Count() == 0)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.DamageGround && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 120))
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
                foreach (var baseLocation in BaseData.BaseLocations.Where(b => b.ResourceCenter == null || b.ResourceCenter.Alliance != SC2APIProtocol.Alliance.Self).Reverse())
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
