namespace Sharky
{
    public class SharkyBot : ISharkyBot
    {
        List<IManager> Managers;
        DebugService DebugService;
        FrameToTimeConverter FrameToTimeConverter;
        SharkyOptions SharkyOptions;
        PerformanceData PerformanceData;
        ChatService ChatService;
        TagService TagService;

        List<SC2Action> Actions;

        DateTime StartTime;

        public SharkyBot(List<IManager> managers, DebugService debugService, FrameToTimeConverter frameToTimeConverter, SharkyOptions sharkyOptions, PerformanceData performanceData, ChatService chatService, TagService tagService)
        {
            Managers = managers;
            DebugService = debugService;
            FrameToTimeConverter = frameToTimeConverter;
            PerformanceData = performanceData;

            SharkyOptions = sharkyOptions;
            ChatService = chatService;
            TagService = tagService;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            Console.WriteLine($"Game Version: {pingResponse.GameVersion}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var manager in Managers)
            {
                manager.OnStart(gameInfo, data, pingResponse, observation, playerId, opponentId);
            }

            stopwatch.Stop();
            Console.WriteLine($"OnStart: {stopwatch.ElapsedMilliseconds} ms");

            PerformanceData.TotalFrameCalculationTime = 0;

            TagService.TagVersion();

            StartTime = DateTime.Now;
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            foreach (var manager in Managers)
            {
                manager.OnEnd(observation, result);
            }

            Console.WriteLine($"Result: {result}");
            var frames = 150;
            if (observation != null)
            {
                frames = (int)observation.Observation.GameLoop;
            }
            var elapsedTime = FrameToTimeConverter.GetTime(frames);
            var elapsedRealTime = DateTime.Now - StartTime;

            Console.WriteLine($"Total Frames: {frames}, elapsed game time: {elapsedTime}, real time: {elapsedRealTime.ToString(@"hh\:mm\:ss")}, {Math.Round(elapsedTime.TotalSeconds / (double)elapsedRealTime.TotalSeconds, 2):f2}X speed, {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MiB memory used");
            Console.WriteLine($"Average Frame Time: {Math.Round(PerformanceData.TotalFrameCalculationTime / frames)} ms, game: {Math.Round(elapsedRealTime.TotalMilliseconds / frames)} ms ({Math.Round(frames / (double)elapsedRealTime.TotalSeconds)} fps)");

        }

        public IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            Actions = new List<SC2APIProtocol.Action>();

            var begin = Stopwatch.GetTimestamp();

            try
            {
                foreach (var manager in Managers)
                {
                    if (!manager.NeverSkip && manager.SkipFrame)
                    {
                        manager.SkipFrame = false;
                        continue;
                    }
                    var beginManager = Stopwatch.GetTimestamp();
                    try
                    {
                        var actions = manager.OnFrame(observation);
                        if (actions != null)
                        {
                            Actions.AddRange(actions);

                            foreach (var action in actions)
                            {
                                if (action?.ActionRaw?.UnitCommand?.UnitTags != null)
                                {
                                    foreach (var tag in action.ActionRaw.UnitCommand.UnitTags)
                                    {
                                        if (!observation.Observation.RawData.Units.Any(u => u.Tag == tag))
                                        {
                                            Console.WriteLine($"{observation.Observation.GameLoop} {manager.GetType().Name}, order {action.ActionRaw.UnitCommand.AbilityId}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        TagService.TagException();
                        Console.WriteLine(exception.ToString());
                    }


                    var endManager = Stopwatch.GetTimestamp();
                    var managerTime = (endManager - beginManager) / (double)Stopwatch.Frequency * 1000.0;
                    manager.TotalFrameTime += managerTime;

                    if (managerTime > 1 && observation.Observation.GameLoop > 100)
                    {
                        manager.SkipFrame = true;

                        if (SharkyOptions.LogPerformance && managerTime > manager.LongestFrame)
                        {
                            manager.LongestFrame = managerTime;
                            Console.WriteLine($"{observation.Observation.GameLoop} {manager.GetType().Name} {managerTime:F2}ms, average: {(manager.TotalFrameTime / observation.Observation.GameLoop):F2}ms");
                        }
                    }
                }

                var end = Stopwatch.GetTimestamp();
                var endTime = (end - begin) / (double)Stopwatch.Frequency * 1000.0;
                PerformanceData.TotalFrameCalculationTime += endTime;
                DebugService.DrawText($"OnFrame: {(int)endTime}");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }

            if (Actions.Any(a => a == null))
            {
                Actions.RemoveAll(a => a == null);
            }

            return Actions;
        }
    }
}
