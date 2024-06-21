namespace Sharky.Builds.BuildChoosing
{
    public class RecentBuildByMapDecisionService: RecentBuildDecisionService
    {
        protected BuildMatcher BuildMatcher;

        public RecentBuildByMapDecisionService(DefaultSharkyBot defaultSharkyBot) 
            : base(defaultSharkyBot) 
        { 
            BuildMatcher = defaultSharkyBot.BuildMatcher;
        }

        public override List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            List<string> debugMessage = new List<string>();
            debugMessage.Add($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");
            Console.WriteLine($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");

            var relevantGames = enemyBot.Games.Where(g => g.EnemyRace == enemyRace && g.MyRace == myRace).ToList();

            return GetBestRecentBuild(relevantGames, enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
        }

        protected override List<string> GetBestRecentBuild(List<Game> relevantGames, EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            var bestBuild = GetUndefeatedBuildForThisMap(relevantGames, enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
            if (bestBuild != null)
            {
                return bestBuild;
            }

            return base.GetBestRecentBuild(relevantGames, enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
        }

        private List<string> GetUndefeatedBuildForThisMap(List<Game> relevantGames, EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            var losses = new List<Game>();
            foreach (var game in relevantGames.Where(g => g.MapName == map))
            {
                if (game.Result == (int)Result.Victory)
                {
                    var sequence = buildSequences.FirstOrDefault(b => BuildMatcher.MatchesBuildSequence(game, b));
                    if (sequence != null && !losses.Any(loss => SameBuildSequence(buildSequences, loss, sequence)))
                    {
                        Console.WriteLine($"Chosen Build Sequence: {string.Join(" ", sequence)}");
                        return sequence;
                    }
                }
                else
                {
                    losses.Add(game);
                }
            }
            return null;
        }
    }
}
