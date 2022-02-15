using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sharky
{
    public class SharkyBot : ISharkyBot
    {
        List<IManager> Managers;
        DebugService DebugService;
        FrameToTimeConverter FrameToTimeConverter;

        List<SC2APIProtocol.Action> Actions;

        Stopwatch Stopwatch;
        Stopwatch ManagerStopwatch;

        double TotalFrameTime;

        public SharkyBot(List<IManager> managers, DebugService debugService, FrameToTimeConverter frameToTimeConverter)
        {
            Managers = managers;
            DebugService = debugService;
            FrameToTimeConverter = frameToTimeConverter;

            Stopwatch = new Stopwatch();
            ManagerStopwatch = new Stopwatch();
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

            TotalFrameTime = 0;
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
            Console.WriteLine($"Total Frames: {frames} {FrameToTimeConverter.GetTime(frames)}");
            Console.WriteLine($"Average Frame Time: {TotalFrameTime/ frames}");
        }

        public IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            Actions = new List<SC2APIProtocol.Action>();

            //Stopwatch.Restart();
            var begin = DateTime.UtcNow;

            try
            {
                foreach (var manager in Managers)
                {
                    if (!manager.NeverSkip && manager.SkipFrame)
                    {
                        manager.SkipFrame = false;
                        //DebugService.DrawText($"{manager.GetType().Name}: skipped");
                        continue;
                    }
                    var beginManager = DateTime.UtcNow;
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

                    var endManager = DateTime.UtcNow;
                    var managerTime = (endManager - beginManager).TotalMilliseconds;
                    //DebugService.DrawText($"{manager.GetType().Name}: {managerTime}");

                    if (managerTime > 1)
                    {
                        manager.SkipFrame = true;
                    }
                    if (managerTime > 1000)
                    {
                        System.Console.WriteLine($"{observation.Observation.GameLoop} {manager.GetType().Name} {managerTime}");
                        //DebugService.DrawText($"{manager.GetType().Name}: {managerTime}");
                    }
                }

                //Stopwatch.Stop();
                //DebugService.DrawText($"OnFrame: {Stopwatch.ElapsedMilliseconds}");
                var end = DateTime.UtcNow;
                var endTime = (end - begin).TotalMilliseconds;
                TotalFrameTime += endTime;
                DebugService.DrawText($"OnFrame: {endTime}");
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
