namespace Sharky.MicroTasks.Scout
{
    public class FindHiddenBaseTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        IIndividualMicroController IndividualMicroController;

        int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }
        List<ScoutInfo> ScoutInfos { get; set; }

        public FindHiddenBaseTask(BaseData baseData, TargetingData targetingData, MapDataService mapDataService, IIndividualMicroController individualMicroController, int desiredCount = 50, bool enabled = true, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            MapDataService = mapDataService;
            IndividualMicroController = individualMicroController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.IsFlying || commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
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

            foreach (var scoutInfo in ScoutInfos.Where(s => s.Harassers.Any()))
            {
                foreach (var commander in scoutInfo.Harassers)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(scoutInfo.Location.X, scoutInfo.Location.Y)) < 4)
                    {
                        var action = IndividualMicroController.Scout(commander, scoutInfo.Location, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => Vector2.DistanceSquared(new Vector2(scoutInfo.Location.X, scoutInfo.Location.Y), e.Position) < 100))
                        {
                            scoutInfo.LastClearedFrame = frame;
                        }
                    }
                    else
                    {
                        var action = IndividualMicroController.Scout(commander, scoutInfo.Location, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
                if (scoutInfo.LastClearedFrame == frame)
                {
                    scoutInfo.Harassers.Clear();
                }
            }
            foreach (var harassInfo in HarassInfos)
            {
                foreach (var commander in harassInfo.Harassers)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 4)
                    {
                        var action = IndividualMicroController.Scout(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            harassInfo.LastClearedFrame = frame;
                        }
                    }
                    else
                    {
                        var action = IndividualMicroController.Scout(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
                if (harassInfo.LastClearedFrame == frame)
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

            if (ScoutInfos == null)
            {
                ScoutInfos = new List<ScoutInfo>();
                ScoutInfos.Add(new ScoutInfo { Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1, Location = new SC2APIProtocol.Point2D { X = 5, Y = 5 } });
                ScoutInfos.Add(new ScoutInfo { Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1, Location = new SC2APIProtocol.Point2D { X = MapDataService.MapData.MapWidth - 5, Y = 5 } });
                ScoutInfos.Add(new ScoutInfo { Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1, Location = new SC2APIProtocol.Point2D { X = 5, Y = MapDataService.MapData.MapHeight - 5 } });
                ScoutInfos.Add(new ScoutInfo { Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1, Location = new SC2APIProtocol.Point2D { X = MapDataService.MapData.MapWidth - 5, Y = MapDataService.MapData.MapHeight - 5 } });
            }
            else
            {
                foreach (var scoutInfo in ScoutInfos)
                {
                    scoutInfo.Harassers.RemoveAll(h => !UnitCommanders.Any(u => u.UnitCalculation.Unit.Tag == h.UnitCalculation.Unit.Tag));
                }
            }

            var unasignedCommanders = UnitCommanders.Where(u => !HarassInfos.Any(info => info.Harassers.Any(h => h.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag) || ScoutInfos.Any(info => info.Harassers.Any(h => h.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag)))).ToList();

            foreach (var scoutInfo in ScoutInfos)
            {
                if (unasignedCommanders.Any())
                {
                    foreach (var info in ScoutInfos.Where(h => h.Harassers.Count() == 0).OrderBy(i => i.LastClearedFrame))
                    {
                        var commander = unasignedCommanders.FirstOrDefault(c => c.UnitCalculation.Unit.IsFlying);
                        if (commander != null)
                        {
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

            if (HarassInfos.Any())
            {
                while (unasignedCommanders.Any())
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
