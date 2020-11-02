namespace Sharky.Builds
{
    public class BuildOptions
    {
        public bool StrictWorkerCount { get; set; }
        public bool StrictSupplyCount { get; set; }
        public bool StrictGasCount { get; set; }
        public ProtossBuildOptions ProtossBuildOptions { get; set; }

        public BuildOptions()
        {
            StrictWorkerCount = false;
            StrictSupplyCount = false;
            StrictGasCount = false;
            ProtossBuildOptions = new ProtossBuildOptions { PylonsAtDefensivePoint = 0, ShieldsAtDefensivePoint = 0 };
        }
    }
}
