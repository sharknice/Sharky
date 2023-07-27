namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// To use quick build orders you need to call base.StartBuild and base.OnFrame at start of those functions, if your build overrides them. <see cref="ZergSharkyBuild"/> as example.
    /// </summary>
    public abstract class QuickBuild : SharkyBuild
    {
        protected QuickBuildOrders QuickBuildOrders = null;
        protected QuickBuildFollower QuickBuildFollower;

        public QuickBuild(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            QuickBuildFollower = new QuickBuildFollower(defaultSharkyBot);
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            if (QuickBuildOrders != null)
            {
                QuickBuildFollower?.Start(QuickBuildOrders);
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            // Try later quickbuild init in case the user defined it in other place than build constructor
            if (QuickBuildOrders != null && !QuickBuildFollower.HasBuild)
            {
                QuickBuildFollower?.Start(QuickBuildOrders);
            }

            QuickBuildFollower?.BuildFrame((int)observation.Observation.GameLoop);
        }
    }
}
