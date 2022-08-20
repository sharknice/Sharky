namespace Sharky.Builds.Zerg
{
    public class ZergBuildOptions
    {
        /// <summary>
        /// If true, creep spread priority is to connect main with natural.
        /// </summary>
        public bool ConnectMainWithNatWithCreep { get; set; } = false;
        
        /// <summary>
        /// How many first tumors have increased score for forward tumors.
        /// </summary>
        public int TumorsPreferForward { get; set; } = 3;

        /// <summary>
        /// If true, second born queen first spawns creep tumor at natural instead of inject, this speeds up the creep spread.
        /// </summary>
        public bool SecondQueenPreferCreepFirst { get; set; } = true;

        /// <summary>
        /// If true, drone is prepositioned for natural hatchery. Works only when  <see cref="QuickBuildOrders">QuickBuildOrders</see> is used for build.
        /// </summary>
        public bool PrepositionDroneForNaturalHatch { get; set; } = true;

        /// <summary>
        /// Maximum count of queens simultaneously spreadiong creep.
        /// </summary>
        public int MaxCreepQueens { get; set; } = 6;

        /// <summary>
        /// Zerglings blocking enemy expansions
        /// </summary>
        public int MaxBurrowedBlockingZerglings { get; set; } = 2;
    }
}
