using SC2APIProtocol;
using Sharky.Chat;
using Sharky.EnemyPlayer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.BuildChoosing
{
    public class RecentBuildDecisionService : BuildDecisionService
    {
        public RecentBuildDecisionService(ChatService chatService, EnemyPlayerService enemyPlayerService) : base(chatService, enemyPlayerService) { }

        private bool SameBuildSequence(List<List<string>> buildSequences, Game originalGame, List<string> currentSequence)
        {
            var originalSequence = buildSequences.FirstOrDefault(b => MatchesBuildSequence(originalGame, b));
            if (originalSequence == null)
            {
                Console.WriteLine($"Original game didn't match any existing build sequences: {string.Join(" ", originalGame.Builds)}");
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

        private List<string> GetBestRecentBuild(List<Game> relevantGames, EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            // find a build we've won with and haven't lost with
            var losses = new List<Game>();
            foreach (var game in relevantGames)
            {
                if (game.Result == (int)Result.Victory)
                {
                    var sequence = buildSequences.FirstOrDefault(b => MatchesBuildSequence(game, b));
                    if (sequence == null)
                    {
                        Console.WriteLine($"Game didn't match any existing build sequences: {string.Join(" ", game.Builds)}");
                    }
                    else if (!losses.Any(loss => SameBuildSequence(buildSequences, loss, sequence)))
                    {
                        Console.WriteLine($"Chosen Build Sequence: {string.Join(" ", sequence)}");
                        return sequence;
                    }
                    else
                    {
                        //Console.WriteLine($"Lost with: {string.Join(" ", sequence)}");
                    }
                }
                else
                {
                    losses.Add(game);
                }
            }

            Console.WriteLine($"No wins we haven't lost with");

            // use a build we haven't lost with yet
            foreach (var sequence in buildSequences)
            {
                if (!losses.Any(loss => SameBuildSequence(buildSequences, loss, sequence)))
                {
                    Console.WriteLine($"Chosen Build Sequence: {string.Join(" ", sequence)}");
                    return sequence;
                }
                else
                {
                    //Console.WriteLine($"Lost with: {string.Join(" ", sequence)}");
                }
            }

            Console.WriteLine($"Lost with every build");

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
