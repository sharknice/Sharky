﻿namespace Sharky.Managers
{
    public interface IManager
    {
        bool NeverSkip { get; set; }
        bool SkipFrame { get; set; }
        double LongestFrame { get; set; }
        double TotalFrameTime { get; set; }
        IEnumerable<SC2Action> OnFrame(ResponseObservation observation);
        void OnEnd(ResponseObservation observation, Result result);
        void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, String opponentId);
    }
}
