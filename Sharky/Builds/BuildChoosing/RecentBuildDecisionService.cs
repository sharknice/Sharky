﻿namespace Sharky.Builds.BuildChoosing
{
    public class RecentBuildDecisionService : BuildDecisionService
    {
        protected BuildMatcher BuildMatcher;

        public RecentBuildDecisionService(DefaultSharkyBot defaultSharkyBot) 
            : base(defaultSharkyBot) 
        { 
            BuildMatcher = defaultSharkyBot.BuildMatcher;
        }

        protected bool SameBuildSequence(List<List<string>> buildSequences, Game originalGame, List<string> currentSequence)
        {
            var originalSequence = buildSequences.FirstOrDefault(b => BuildMatcher.MatchesBuildSequence(originalGame, b));
            if (originalSequence == null)
            {
                return false;
            }
            if (originalSequence == currentSequence)
            {
                return true;
            }
            return false;
        }

        public override List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            List<string> debugMessage = new List<string>();
            debugMessage.Add($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");
            Console.WriteLine($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");

            var relevantGames = enemyBot.Games.Where(g => g.EnemyRace == enemyRace && g.MyRace == myRace).ToList();

            return GetBestRecentBuild(relevantGames, enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
        }

        protected virtual List<string> GetBestRecentBuild(List<Game> relevantGames, EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            // find a build we've won with and haven't lost with
            var losses = new List<Game>();
            foreach (var game in relevantGames)
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

            // use a build we haven't lost with yet
            foreach (var sequence in buildSequences)
            {
                if (!losses.Any(loss => SameBuildSequence(buildSequences, loss, sequence)))
                {
                    Console.WriteLine($"Chosen Build Sequence: {string.Join(" ", sequence)}");
                    return sequence;
                }
            }


            // lost with every build
            // keep removing the last game from the list and try this whole thing over again
            if (relevantGames.Count > 0)
            {
                relevantGames.RemoveAt(relevantGames.Count - 1);
                return GetBestRecentBuild(relevantGames, enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
            }
            else
            {
                return buildSequences.FirstOrDefault();
            }
        }
    }
}
