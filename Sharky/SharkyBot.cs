using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky
{
    public class SharkyBot : ISharkyBot
    {
        public SharkyBot()
        {
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            Console.WriteLine($"Game Version: {pingResponse.GameVersion}");

            throw new NotImplementedException();
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            throw new NotImplementedException();
        }
    }
}
