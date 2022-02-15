namespace Sharky.Builds.BuildingPlacement
{
    public enum WallOffType
    {
        None,
        Partial,
        /// <summary>
        /// full wall with supply depots
        /// </summary>
        Full,
        /// <summary>
        /// use a combination of supply depots and production structures
        /// </summary>
        Terran
    }
}
