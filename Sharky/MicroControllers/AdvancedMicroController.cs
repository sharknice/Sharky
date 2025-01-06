using Sharky.Concaves;

namespace Sharky.MicroControllers
{
    public class AdvancedMicroController : IMicroController
    {
        float GroupUpDistanceSquared;
        float GroupUpCompletedDistanceSquared;

        MicroData MicroData;
        SharkyOptions SharkyOptions;
        MapDataService MapDataService;

        ConcaveService ConcaveService;
        ConcaveGroupData ConcaveGroupData;

        public bool GroupingEnabled { get; set; }

        public AdvancedMicroController(DefaultSharkyBot defaultSharkyBot)
        {
            MicroData = defaultSharkyBot.MicroData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            MapDataService = defaultSharkyBot.MapDataService;

            GroupUpDistanceSquared = 200;
            GroupUpCompletedDistanceSquared = 64;

            ConcaveService = new ConcaveService(defaultSharkyBot);
            ConcaveGroupData = new ConcaveGroupData();
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

        public List<SC2Action> Defend(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
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
                    action = individualMicroController.Defend(commander, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Defend(commander, target, defensivePoint, groupCenter, frame);
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

                action = RetreatWithCommander(defensivePoint, groupCenter, frame, commander);
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }

        private List<SC2Action> RetreatWithCommander(Point2D defensivePoint, Point2D groupCenter, int frame, UnitCommander commander)
        {
            if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
            {
                return individualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
            }
            else
            {
                return MicroData.IndividualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
            }
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

        public List<SC2APIProtocol.Action> Contain(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (groupCenter == null)
            {
                return Retreat(commanders, defensivePoint, groupCenter, frame);
            }

            UpdateConcave(commanders, target, defensivePoint, groupCenter, frame);        

            var actions = new List<SC2APIProtocol.Action>();

            if (ConcaveGood(ConcaveGroupData))
            {
                foreach (var commander in commanders)
                {
                    if (commander.LastOrderFrame == frame) { continue; }
                    List<SC2APIProtocol.Action> action;
                    commander.CommanderState = CommanderState.None;
                    Point2D assignedPosition = null;

                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS)
                    {
                        if (ConcaveGroupData.HybridConcavePoints != null && ConcaveGroupData.HybridConcavePoints.ContainsKey(commander.UnitCalculation.Unit.Tag))
                        {
                            assignedPosition = ConcaveGroupData.HybridConcavePoints[commander.UnitCalculation.Unit.Tag].ToPoint2D();
                        }
                    }
                    else if (!commander.UnitCalculation.Unit.IsFlying)
                    {
                        if (ConcaveGroupData.GroundConcavePoints != null && ConcaveGroupData.GroundConcavePoints.ContainsKey(commander.UnitCalculation.Unit.Tag))
                        {
                            assignedPosition = ConcaveGroupData.GroundConcavePoints[commander.UnitCalculation.Unit.Tag].ToPoint2D();
                        }
                    }
                    else
                    {
                        if (ConcaveGroupData.AirConcavePoints != null && ConcaveGroupData.AirConcavePoints.ContainsKey(commander.UnitCalculation.Unit.Tag))
                        {
                            assignedPosition = ConcaveGroupData.AirConcavePoints[commander.UnitCalculation.Unit.Tag].ToPoint2D();
                        }
                    }

                    if (assignedPosition != null)
                    {
                        action = ContainWithCommander(defensivePoint, groupCenter, frame, actions, commander, assignedPosition);
                    }
                    else
                    {
                        action = RetreatWithCommander(defensivePoint, groupCenter, frame, commander);
                    }
                    if (action != null) { actions.AddRange(action); }
                }

                return actions;
            }

            if (ConcaveGroupData.Threat == null && !commanders.Any(c => c.UnitCalculation.EnemiesInRangeOfAvoid.Any()))
            {
                return Attack(commanders, target, defensivePoint, groupCenter, frame);
            }

            return Retreat(commanders, defensivePoint, groupCenter, frame);
        }

        private List<SC2Action> ContainWithCommander(Point2D defensivePoint, Point2D groupCenter, int frame, List<SC2Action> actions, UnitCommander commander, Point2D assignedPosition)
        {
            if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
            {
                return individualMicroController.Contain(commander, assignedPosition, defensivePoint, groupCenter, frame);
            }
            else
            {
                return MicroData.IndividualMicroController.Contain(commander, assignedPosition, defensivePoint, groupCenter, frame);
            }
        }

        private bool ConcaveGood(ConcaveGroupData concaveGroupData)
        {
            if (concaveGroupData.GroundConcavePoints == null || concaveGroupData.AirConcavePoints == null || concaveGroupData.HybridConcavePoints == null) { return false; }

            var ground = concaveGroupData.UnitCommanders.Where(c => !c.UnitCalculation.Unit.IsFlying && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS);
            var flying = concaveGroupData.UnitCommanders.Where(c => c.UnitCalculation.Unit.IsFlying);
            var hybrid = concaveGroupData.UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS);

            if (ground.Count() != concaveGroupData.GroundConcavePoints.Count) { return false; }
            if (flying.Count() != concaveGroupData.AirConcavePoints.Count) { return false; }
            if (hybrid.Count() != concaveGroupData.HybridConcavePoints.Count) { return false; }

            if (ground.Any(p => !concaveGroupData.GroundConcavePoints.ContainsKey(p.UnitCalculation.Unit.Tag))) { return false; }
            if (flying.Any(p => !concaveGroupData.AirConcavePoints.ContainsKey(p.UnitCalculation.Unit.Tag))) { return false; }
            if (hybrid.Any(p => !concaveGroupData.HybridConcavePoints.ContainsKey(p.UnitCalculation.Unit.Tag))) { return false; }

            if (concaveGroupData.GroundConcavePoints.Values.Any(p => MapDataService.EnemyGroundDpsInRange(p.ToPoint2D()) > 0)) { return false; }
            if (concaveGroupData.AirConcavePoints.Values.Any(p => MapDataService.EnemyAirDpsInRange(p) > 0)) { return false; }
            if (concaveGroupData.HybridConcavePoints.Values.Any(p => MapDataService.EnemyGroundDpsInRange(p.ToPoint2D()) > 0 || MapDataService.EnemyAirDpsInRange(p) > 0)) { return false; }

            return true;
        }

        private void UpdateConcave(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            ConcaveGroupData.UnitCommanders = commanders.ToList();

            var centerCommander = commanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, groupCenter.ToVector2())).FirstOrDefault();
            if (centerCommander == null) { return; }

            var oldThreat = -1;
            if (ConcaveGroupData.Threat != null) { oldThreat = (int)ConcaveGroupData.Threat.Unit.Tag; }
            ConcaveGroupData.Threat = ConcaveService.GetThreat(centerCommander);
            if (ConcaveGroupData.Threat == null) { return; }

            if (oldThreat == (int)ConcaveGroupData.Threat.Unit.Tag) 
            {
                if (ConcaveGood(ConcaveGroupData))
                {
                    return;
                }
            }

            ConcaveGroupData.ConvergePoint = ConcaveGroupData.Threat.Position.ToPoint2D();
            var distance = ConcaveGroupData.Threat.Range + 4;

            ConcaveGroupData.AirConcavePoints = new Dictionary<ulong, Vector2>();
            ConcaveGroupData.GroundConcavePoints = new Dictionary<ulong, Vector2>();
            ConcaveGroupData.HybridConcavePoints = new Dictionary<ulong, Vector2>();

            var closestCommander = commanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, ConcaveGroupData.Threat.Position)).FirstOrDefault();
            if (Vector2.Distance(centerCommander.UnitCalculation.Position, ConcaveGroupData.Threat.Position) - Vector2.Distance(closestCommander.UnitCalculation.Position, ConcaveGroupData.Threat.Position) > 2)
            {
                centerCommander = closestCommander;
            }
            var referencePosition = ConcaveService.GetPositionFromRange(ConcaveGroupData.ConvergePoint.X, ConcaveGroupData.ConvergePoint.Y, centerCommander.UnitCalculation.Position.X, centerCommander.UnitCalculation.Position.Y, distance);

            var groundCommanders = commanders.Where(c => !c.UnitCalculation.Unit.IsFlying && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS);
            if (groundCommanders.Any())
            {
                var spreadDistance = groundCommanders.OrderByDescending(c => c.UnitCalculation.Unit.Radius).FirstOrDefault().UnitCalculation.Unit.Radius * 2;
                var extraDistance = 0f;
                var goodSpots = new List<Vector2>();
                while (goodSpots.Count < groundCommanders.Count() && extraDistance < spreadDistance * 10)
                {
                    goodSpots.AddRange(ConcaveService.GetConcavePositions(ConcaveGroupData.ConvergePoint.ToVector2(), referencePosition, spreadDistance, distance + extraDistance, groundCommanders.Count() - goodSpots.Count, true, false));
                    extraDistance += spreadDistance;
                }
                if (goodSpots.Count >= groundCommanders.Count())
                {
                    ConcaveService.AssignCommandersToConcavePositions(ConcaveGroupData, groundCommanders, goodSpots, true, false, false);
                }
            }
 
            var airCommanders = commanders.Where(c => c.UnitCalculation.Unit.IsFlying);
            if (airCommanders.Any())
            {
                var spreadDistance = airCommanders.OrderByDescending(c => c.UnitCalculation.Unit.Radius).FirstOrDefault().UnitCalculation.Unit.Radius * 2;
                var goodSpots = ConcaveService.GetConcavePositions(ConcaveGroupData.ConvergePoint.ToVector2(), referencePosition, spreadDistance, distance, airCommanders.Count(), false, true);
                if (goodSpots.Count < airCommanders.Count())
                {
                    goodSpots.AddRange(ConcaveService.GetConcavePositions(ConcaveGroupData.ConvergePoint.ToVector2(), referencePosition, spreadDistance, distance + 3, airCommanders.Count() - goodSpots.Count, false, true));
                }
                if (goodSpots.Count >= airCommanders.Count())
                {
                    ConcaveService.AssignCommandersToConcavePositions(ConcaveGroupData, airCommanders, goodSpots, false, true, false);
                }
            }

            var hybridCommanders = commanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS);
            if (hybridCommanders.Any())
            {
                var spreadDistance = hybridCommanders.OrderByDescending(c => c.UnitCalculation.Unit.Radius).FirstOrDefault().UnitCalculation.Unit.Radius * 2;
                var goodSpots = ConcaveService.GetConcavePositions(ConcaveGroupData.ConvergePoint.ToVector2(), referencePosition, spreadDistance, distance + 2, hybridCommanders.Count(), true, true);
                if (goodSpots.Count >= hybridCommanders.Count())
                {
                    ConcaveService.AssignCommandersToConcavePositions(ConcaveGroupData, hybridCommanders, goodSpots, false, false, true);
                }
            }
        }
    }
}
