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
    }
}
