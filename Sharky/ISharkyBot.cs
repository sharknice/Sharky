namespace Sharky
{
    public interface ISharkyBot
    {
        IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation);
        void OnEnd(ResponseObservation observation, Result result);
        void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, String opponentId);
    }
}
