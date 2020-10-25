using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;

namespace Sharky
{
    public class SharkyBot : ISharkyBot
    {
        List<IManager> Managers;
        List<SC2APIProtocol.Action> Actions;

        public SharkyBot(List<IManager> managers)
        {
            Managers = managers;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            Console.WriteLine($"Game Version: {pingResponse.GameVersion}");

            foreach (var manager in Managers)
            {
                manager.OnStart(gameInfo, data, pingResponse, observation, playerId, opponentId);
            }
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

            return Actions;
        }
    }
}
