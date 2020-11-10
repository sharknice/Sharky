using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sharky
{
    public class SharkyBotAsync : ISharkyBotAsync
    {
        List<IManager> Managers;
        DebugManager DebugManager;
        List<SC2APIProtocol.Action> Actions;

        public SharkyBotAsync(List<IManager> managers, DebugManager debugManager)
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

        public async Task<IEnumerable<SC2APIProtocol.Action>> OnFrame(ResponseObservation observation)
        {
            Actions = new List<SC2APIProtocol.Action>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                foreach (var manager in Managers)
                {
                    Actions.AddRange(manager.OnFrame(observation));
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }

            stopwatch.Stop();
            DebugManager.DrawText($"OnFrame: {stopwatch.ElapsedMilliseconds}");

            return Actions;
        }
    }
}
