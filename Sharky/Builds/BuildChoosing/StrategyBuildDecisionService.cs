using Sharky.Chat;
using Sharky.EnemyPlayer;

namespace Sharky.Builds.BuildChoosing
{
    /// <summary>
    /// TODO: Looks at the enemy strategies used in the last game and attempts to find the best counter
    /// </summary>
    public class StrategyBuildDecisionService : BuildDecisionService
    {
        protected BuildMatcher BuildMatcher;

        public StrategyBuildDecisionService(ChatService chatService, EnemyPlayerService enemyPlayerService, RecordService recordService, BuildMatcher buildMatcher) 
            : base(chatService, enemyPlayerService, recordService) 
        { 
            BuildMatcher = buildMatcher;
        }
    }
}
