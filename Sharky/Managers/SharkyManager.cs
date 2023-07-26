namespace Sharky.Managers
{
    public abstract class SharkyManager : IManager
    {
        public virtual double TotalFrameTime { get; set; }
        public virtual double LongestFrame { get; set; }
        public virtual bool SkipFrame { get; set; }
        public virtual bool NeverSkip { protected set { } get { return false; } }

        public virtual void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {

        }

        public virtual IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            return new List<SC2Action>();
        }

        public virtual void OnEnd(ResponseObservation observation, Result result)
        {
            if (observation != null)
            {
                System.Console.WriteLine($"{observation.Observation.GameLoop} {GetType().Name} {TotalFrameTime:F2}ms, average: {(TotalFrameTime / observation.Observation.GameLoop):F2}ms");
            }
            else
            {
                System.Console.WriteLine("OnEnd Observation null");
            }
        }
    }
}
