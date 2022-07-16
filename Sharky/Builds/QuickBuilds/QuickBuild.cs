using SC2APIProtocol;
using Sharky.DefaultBot;

namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// To use quick build orders you need to call base.StartBuild and base.OnFrame at start of those functions, if your build overrides them. <see cref="ZergSharkyBuild"/> as example.
    /// </summary>
    public class QuickBuild : SharkyBuild
    {
        protected QuickBuildOrders QuickBuildOrders = null;
        protected QuickBuildFollower QuickBuildFollower;

        public QuickBuild(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            QuickBuildFollower = new QuickBuildFollower(defaultSharkyBot);
        }

        public override void StartBuild(int frame)
        {
            QuickBuildFollower?.Start(QuickBuildOrders);
        }

        public override void OnFrame(ResponseObservation observation)
        {
            QuickBuildFollower?.BuildFrame((int)observation.Observation.GameLoop);
        }
    }
}
