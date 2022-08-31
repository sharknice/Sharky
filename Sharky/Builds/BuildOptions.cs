using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.Zerg;

namespace Sharky.Builds
{
    public class BuildOptions
    {
        public bool StrictWorkerCount { get; set; }
        public bool StrictSupplyCount { get; set; }
        public bool EncroachEnemyMainWithExpansions { get; set; }
        public bool StrictGasCount { get; set; }
        public bool StrictWorkersPerGas { get; set; }
        public int StrictWorkersPerGasCount { get; set; }
        public int MaxActiveGasCount { get; set; }
        public ZergBuildOptions ZergBuildOptions { get; set; }
        public WallOffType WallOffType { get; set; }
        
        /// <summary>
        /// allows buildings that are not part of the wall to be built next to the wall and potentially interfere with it
        /// </summary>
        public bool AllowBlockWall { get; set; }

        public BuildOptions()
        {
            StrictWorkerCount = false;
            StrictSupplyCount = false;
            StrictGasCount = false;
            StrictWorkersPerGas = false;
            EncroachEnemyMainWithExpansions = false;
            AllowBlockWall = false;
            MaxActiveGasCount = 8;
            StrictWorkersPerGasCount = 3;
            ZergBuildOptions = new ZergBuildOptions { ConnectMainWithNatWithCreep = false };
            WallOffType = WallOffType.None;
        }
    }
}
