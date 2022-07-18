namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// Custom actions for QuickBuild
    /// </summary>
    public enum QuickAction
    {
        /// <summary>
        /// If there is extractor being built, it gets cancelled. This can be used to get extra supply for zerg.
        /// Currently works only for non-rich gas extractors.
        /// </summary>
        DoExtractorTrick,
    }
}
