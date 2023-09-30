namespace Sharky.MicroControllers
{
    public class AdvancedMicroController : IMicroController
    {
        float GroupUpDistanceSquared;
        float GroupUpCompletedDistanceSquared;

        MicroData MicroData;
        SharkyOptions SharkyOptions;
        MapDataService MapDataService;

        public bool GroupingEnabled { get; set; }

        public AdvancedMicroController(DefaultSharkyBot defaultSharkyBot)
        {
            MicroData = defaultSharkyBot.MicroData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            MapDataService = defaultSharkyBot.MapDataService;

            GroupUpDistanceSquared = 200;
            GroupUpCompletedDistanceSquared = 64;
        }

        public List<SC2APIProtocol.Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            Vector2 groupVector = Vector2.Zero;
            var centerHeight = -1;
            if (GroupingEnabled && groupCenter != null && frame > SharkyOptions.FramesPerSecond * 3 * 60)
            {
                centerHeight = MapDataService.MapHeight(groupCenter);
                groupVector = new Vector2(groupCenter.X, groupCenter.Y);
            }

            var groupThreshold = commanders.Count() / 2;
            foreach (var commander in commanders)
            {
                if (commander.LastOrderFrame == frame) { continue; }
                List<SC2APIProtocol.Action> action;
                SetState(commander, groupVector, groupThreshold, centerHeight);
                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }

        public List<SC2APIProtocol.Action> Retreat(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            Vector2 groupVector = Vector2.Zero;
            var centerHeight = -1;
            if (GroupingEnabled && (groupCenter != null))
            {
                centerHeight = MapDataService.MapHeight(groupCenter);
                groupVector = new Vector2(groupCenter.X, groupCenter.Y);
            }

            var groupThreshold = commanders.Count() / 2;
            foreach (var commander in commanders)
            {
                if (commander.LastOrderFrame == frame) { continue; }
                List<SC2APIProtocol.Action> action;
                if (groupCenter != null)
                {
                    SetState(commander, groupVector, groupThreshold, centerHeight);
                }

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }

        public List<SC2APIProtocol.Action> Idle(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in commanders)
            {
                List<SC2APIProtocol.Action> action;
                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Idle(commander, defensivePoint, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Idle(commander, defensivePoint, frame);
                }
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }

        public List<SC2APIProtocol.Action> Support(IEnumerable<UnitCommander> commanders, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var stopwatchTotal = Stopwatch.StartNew();
            var stopwatch1 = Stopwatch.StartNew();

            var targetVector = new Vector2(target.X, target.Y);
            Vector2 groupVector = Vector2.Zero;
            var centerHeight = -1;
            if (GroupingEnabled && groupCenter != null)
            {
                centerHeight = MapDataService.MapHeight(groupCenter);
                groupVector = new Vector2(groupCenter.X, groupCenter.Y);
            }

            if (stopwatch1.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"Support start {stopwatch1.ElapsedMilliseconds}");
            }
            stopwatch1.Restart();

            var groupThreshold = commanders.Count() / 2;
            int index = 0;
            foreach (var commander in commanders)
            {
                if (commander.SkipFrame || commander.LastOrderFrame == frame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                List<SC2APIProtocol.Action> action;
                SetState(commander, groupVector, groupThreshold, centerHeight);

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
                }
                if (action != null) { actions.AddRange(action); }

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 1)
                {
                    commander.SkipFrame = true;
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        System.Console.WriteLine($"{stopwatch.ElapsedMilliseconds} {(UnitTypes)commander.UnitCalculation.Unit.UnitType}");
                    }
                }
                index++;
            }

            if (stopwatchTotal.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"Support end {stopwatchTotal.ElapsedMilliseconds}");
            }

            return actions;
        }

        void SetState(UnitCommander commander, Vector2 groupVector, int groupThreshold, int centerHeight)
        {
            if (!GroupingEnabled) { return; }

            if (groupVector == Vector2.Zero || commander.UnitCalculation.NearbyAllies.Count() > groupThreshold || centerHeight != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos))
            {
                commander.CommanderState = CommanderState.None;
            }
            else if (commander.CommanderState == CommanderState.Grouping)
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, groupVector) < GroupUpCompletedDistanceSquared)
                {
                    commander.CommanderState = CommanderState.None;
                }
            }
            else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, groupVector) > GroupUpDistanceSquared)
            {
                commander.CommanderState = CommanderState.Grouping;
            }
        }

        public List<SC2APIProtocol.Action> AttackWithinArea(IEnumerable<UnitCommander> commanders, List<Point2D> area, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            Vector2 groupVector = Vector2.Zero;
            var centerHeight = -1;
            if (GroupingEnabled && groupCenter != null && frame > SharkyOptions.FramesPerSecond * 3 * 60)
            {
                centerHeight = MapDataService.MapHeight(groupCenter);
                groupVector = new Vector2(groupCenter.X, groupCenter.Y);
            }

            var groupThreshold = commanders.Count() / 2;
            foreach (var commander in commanders)
            {
                if (commander.LastOrderFrame == frame) { continue; }
                List<SC2APIProtocol.Action> action;
                SetState(commander, groupVector, groupThreshold, centerHeight);
                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.AttackWithinArea(commander, area, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.AttackWithinArea(commander, area, target, defensivePoint, groupCenter, frame);
                }
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }
    }
}
