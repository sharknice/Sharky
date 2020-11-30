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
        DebugManager DebugManager;
        List<SC2APIProtocol.Action> Actions;

        public SharkyBot(List<IManager> managers, DebugManager debugManager)
        {
            Managers = managers;
            DebugManager = debugManager;
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
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            foreach (var manager in Managers)
            {
                manager.OnEnd(observation, result);
            }
        }

        public IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            Actions = new List<SC2APIProtocol.Action>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var managerStopwatch = new Stopwatch();
                foreach (var manager in Managers)
                {
                    managerStopwatch.Restart();
                    Actions.AddRange(manager.OnFrame(observation));
                    DebugManager.DrawText($"{manager.GetType().Name}: {managerStopwatch.ElapsedMilliseconds}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }

            stopwatch.Stop();
            DebugManager.DrawText($"OnFrame: {stopwatch.ElapsedMilliseconds}");

            if (Actions.Any(a => a == null))
            {
                Actions.RemoveAll(a => a == null); // TODO: figure out what is adding null actions
            }

            return Actions;
        }
    }
}
