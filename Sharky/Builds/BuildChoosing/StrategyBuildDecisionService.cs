namespace Sharky.Builds.BuildChoosing
{
    /// <summary>
    /// TODO: Looks at the enemy strategies used in the last game and attempts to find the best counter
    /// </summary>
    public class StrategyBuildDecisionService : BuildDecisionService
    {
        protected BuildMatcher BuildMatcher;

        public StrategyBuildDecisionService(DefaultSharkyBot defaultSharkyBot) 
            : base(defaultSharkyBot) 
        { 
            BuildMatcher = defaultSharkyBot.BuildMatcher;
        }
    }
}
