using SC2APIProtocol;
using Sharky.DefaultBot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.BuildChoosing
{
    /// <summary>
    /// Prioritizes a single build
    /// </summary>
    public class PrioritizedBuildDecisionService : RecentBuildDecisionService
    {
        private List<string> PrioritizedBuildSequence;

        public PrioritizedBuildDecisionService(DefaultSharkyBot defaultSharkyBot, List<string> prioritizedBuildSequence) 
            : base(defaultSharkyBot) 
        { 
            BuildMatcher = defaultSharkyBot.BuildMatcher;
            PrioritizedBuildSequence = prioritizedBuildSequence;
        }

        public override List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            Console.Write($"Prioritizing {string.Join(" ", PrioritizedBuildSequence)}");
            Console.WriteLine($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");

            var relevantGames = enemyBot.Games.Where(g => g.EnemyRace == enemyRace && g.MyRace == myRace).ToList();

            var lastGame = enemyBot.Games.Where(g => g.MyRace == myRace && g.EnemySelectedRace == enemyRace).FirstOrDefault();
            if (lastGame != null)
            {
                Console.WriteLine($"{(Result)lastGame.Result} last game with: {string.Join(" ", lastGame.Builds.Values)}");
                if (lastGame.Result == (int)Result.Victory)
                {
                    Console.WriteLine("Won last game, using same build");
                    var sequence = buildSequences.FirstOrDefault(b => BuildMatcher.MatchesBuildSequence(lastGame, b));
                    if (sequence != null)
                    {
                        Console.WriteLine($"choice: {string.Join(" ", sequence)}");
                        return sequence;
                    }
                }

                if (!BuildMatcher.MatchesBuildSequence(lastGame, PrioritizedBuildSequence))
                {
                    var sequenceString = string.Join(" ", PrioritizedBuildSequence);
                    if (buildSequences.Any(b => string.Join(" ", b) == sequenceString))
                    {
                        Console.WriteLine($"choice: {sequenceString}");
                        return PrioritizedBuildSequence;
                    }
                }
            }

            return base.GetBestBuild(enemyBot, buildSequences, map, enemyBots, enemyRace, myRace);
        }
    }
}
