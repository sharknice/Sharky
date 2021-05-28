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
        List<SC2APIProtocol.Action> Actions;

        Stopwatch Stopwatch;
        Stopwatch ManagerStopwatch;

        double TotalFrameTime;

        public SharkyBot(List<IManager> managers, DebugService debugService)
        {
            Managers = managers;
            DebugService = debugService;

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
            
            Console.WriteLine($"Total Frames: {observation.Observation.GameLoop}");
            Console.WriteLine($"Average Frame Time: {TotalFrameTime/ observation.Observation.GameLoop}");
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
                        DebugService.DrawText($"{manager.GetType().Name}: skipped");
                        continue;
                    }
                    //ManagerStopwatch.Restart();
                    var beginManager = DateTime.UtcNow;
                    var actions = manager.OnFrame(observation);
                    if (actions != null)
                    {
                        Actions.AddRange(actions);
                    }

                    var endManager = DateTime.UtcNow;
                    var managerTime = (endManager - beginManager).TotalMilliseconds;
                    DebugService.DrawText($"{manager.GetType().Name}: {managerTime}");

                    //DebugService.DrawText($"{manager.GetType().Name}: {ManagerStopwatch.ElapsedMilliseconds}");
                    //ManagerStopwatch.Stop();
                    //if (manager.GetType().Name == "UnitManager")
                    //    Debug.WriteLine($"{manager.GetType().Name} {ManagerStopwatch.ElapsedMilliseconds}");

                    if (managerTime > 1)
                    //if (ManagerStopwatch.ElapsedMilliseconds > 1)
                    {
                        //if (managerStopwatch.ElapsedMilliseconds > 10)
                        //{
                        //    Debug.WriteLine($"{manager.GetType().Name} {ManagerStopwatch.ElapsedMilliseconds}");
                        //    var lol = true;
                        //}
                        manager.SkipFrame = true;
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
                Actions.RemoveAll(a => a == null); // TODO: figure out what is adding null actions
            }

            return Actions;
        }
    }
}
