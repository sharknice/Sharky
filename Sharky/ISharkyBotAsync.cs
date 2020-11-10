using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sharky
{
    public interface ISharkyBotAsync
    {
        Task<IEnumerable<SC2APIProtocol.Action>> OnFrame(ResponseObservation observation);
        void OnEnd(ResponseObservation observation, Result result);
        void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, String opponentId);
    }
}
