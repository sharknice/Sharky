using SC2APIProtocol;
using Sharky.Builds.Zerg;
using Sharky.DefaultBot;

namespace Sharky.Builds.QuickBuilds
{
    public class ZergQuickBuild : ZergSharkyBuild
    {
        protected QuickBuild QuickBuild = null;
        protected QuickBuildFollower QuickBuildFollower;

        public ZergQuickBuild(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            QuickBuildFollower = new QuickBuildFollower(defaultSharkyBot);
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (QuickBuild != null)
            {
                QuickBuildFollower.Follow(QuickBuild);
            }
        }
    }
}
