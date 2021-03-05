using Sharky.MicroControllers.Protoss;
using Sharky.MicroTasks.Harass;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class AdeptWorkerHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        AdeptMicroController AdeptMicroController;

        int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }

        bool started;

        public AdeptWorkerHarassTask(BaseData baseData, TargetingData targetingData, AdeptMicroController adeptMicroController, int desiredCount = 2, bool enabled = true, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            AdeptMicroController = adeptMicroController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            started = false;
        }

        public override void Enable()
        {
            Enabled = true;
            started = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT))
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    if (UnitCommanders.Count() == DesiredCount)
                    {
                        started = true;
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
                var changeBases = false;
                foreach (var commander in harassInfo.Harassers)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 100)
                    {
                        var action = AdeptMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            harassInfo.LastClearedFrame = frame;
                            changeBases = true;
                            return commands;
                        }
                        else if (commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {                       
                            if (commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability < 1 && commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax)
                            {
                                // TODO: shade out
                                harassInfo.LastDefendedFrame = frame;
                                changeBases = true; 
                                return commands;
                            }
                        }
                    }
                    else if (commander.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying && (e.Unit.Health + e.Unit.Shield < commander.UnitCalculation.Damage) && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 8))  // if undetected and near one hit kills just kill them
                    {
                        var action = AdeptMicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var action = AdeptMicroController.NavigateToPoint(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (commander.RetreatPath.Count() == 0)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Detector) && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 120))
                            {
                                harassInfo.LastPathFailedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                    }
                }
                if (changeBases)
                {
                    harassInfo.Harassers.Clear();
                }
            }

            return commands;
        }

        void AssignHarassers()
        {
            if (HarassInfos == null)
            {
                HarassInfos = new List<HarassInfo>();
                foreach (var baseLocation in BaseData.EnemyBases.Where(b => b.ResourceCenter == null || b.ResourceCenter.Alliance != SC2APIProtocol.Alliance.Self))
                {
                    HarassInfos.Add(new HarassInfo { BaseLocation = baseLocation, Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1 });
                }
            }
            else
            {
                foreach (var baseLocation in BaseData.EnemyBases)
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
                    foreach (var info in HarassInfos.OrderBy(h => HighestFrame(h)))
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
