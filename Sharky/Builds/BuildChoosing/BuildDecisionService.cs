using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.BuildChoosing
{
    public class BuildDecisionService : IBuildDecisionService
    {
        IChatManager ChatManager;

        public BuildDecisionService(IChatManager chatManager)
        {
            ChatManager = chatManager;
        }

        private bool BetterBuild(Record original, Record current)
        {
            if (original == null)
            {
                return true;
            }

            if (original.Wins.Count() > 0 && original.Losses.Count() == 0 && original.Ties.Count() == 0)
            {
                return false; // always win, keep using it
            }

            if (original.Losses.Count() > 0 && current.Losses.Count() == 0 && current.Ties.Count() == 0)
            {
                return true; // not a perfect record, give another build a chance
            }

            if (original.Losses.Count() == 0 && current.Losses.Count() > 0)
            {
                return false; // if it has losses it isn't better than one without losses
            }

            if (WonLastGame(original) && WonLastGame(current))
            {
                return current.Wins.OrderByDescending(x => x).First() < original.Wins.OrderByDescending(x => x).First(); // newer games are a better representation, because enemy bots and this bot update
            }

            if (!WonLastGame(original) && WonLastGame(current))
            {
                return true;
            }

            return false;
        }

        private bool WonLastGame(Record record)
        {
            if (record.Wins.Count() > 0 && record.Losses.Count() == 0)
            {
                return true;
            }
            if (record.Wins.Count() > 0 && record.Losses.Count() > 0)
            {
                return record.Wins.OrderByDescending(x => x).First() < record.Losses.OrderByDescending(x => x).First();
            }
            return false;
        }

        public List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace)
        {
            List<string> debugMessage = new List<string>();
            debugMessage.Add($"Choosing build against {enemyBot.Name} - {enemyBot.Id}");
            Console.WriteLine($"Choosing build against {enemyBot.Name} - {enemyBot.Id}");

            var bestBuildSequence = buildSequences.First();

            var mapGames = enemyBot.Games.Where(g => g.MapName == map).Where(g => g.EnemyRace == enemyRace); // it is possible a bot could be updated and change races on the ladder
            Record bestRecord = null; //GetSequenceRecord(mapGames, bestBuildSequence);
            //Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
            //debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
            var record = GetRecord(mapGames);
            Console.WriteLine($"Same enemy, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"Same enemy, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (mapGames.Count() > 0)
            {
                // check games on this map
                foreach (var buildSequence in buildSequences)
                {
                    var buildRecord = GetSequenceRecord(mapGames, buildSequence);
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            record = GetRecord(enemyBot.Games.Where(g => g.EnemyRace == enemyRace));
            Console.WriteLine($"Same enemy, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"Same enemy, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (bestRecord == null || bestRecord.Wins.Count() == 0)
            {
                // check games on other maps
                foreach (var buildSequence in buildSequences)
                {
                    if (GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                    var buildRecord = GetSequenceRecord(enemyBot.Games.Where(g => g.EnemyRace == enemyRace), buildSequence);
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            record = GetRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace).Where(g => g.MapName == map));
            Console.WriteLine($"All enemies, same race, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"All enemies, same race, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (bestRecord.Wins.Count() == 0)
            {
                // check games on this map from other bots of the same race
                foreach (var buildSequence in buildSequences)
                {
                    if (GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                    var buildRecord = GetSequenceRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace).Where(g => g.MapName == map), buildSequence);
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            record = GetRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace));
            Console.WriteLine($"All enemies, same race, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"All enemies, same race, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (bestRecord.Wins.Count() == 0)
            {
                // check games on other maps from other bots of the same race
                foreach (var buildSequence in buildSequences)
                {
                    if (GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                    var buildRecord = GetSequenceRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace), buildSequence);
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            Console.WriteLine($"Chosen Build Sequence: {string.Join(" ", bestBuildSequence)}");
            debugMessage.Add($"Chosen Build Sequence: {string.Join(" ", bestBuildSequence)}");
            Console.WriteLine($"Record: Wins: {bestRecord.Wins.Count()} Losses: {bestRecord.Losses.Count()} Ties: {bestRecord.Ties.Count()}");
            debugMessage.Add($"Record: Wins: {bestRecord.Wins.Count()} Losses: {bestRecord.Losses.Count()} Ties: {bestRecord.Ties.Count()}");
            //ChatManager.SendDebugChatMessages(debugMessage);

            return bestBuildSequence;
        }

        private Record GetSequenceRecord(IEnumerable<Game> games, List<string> sequence)
        {
            var record = new Record { Wins = new List<DateTime>(), Losses = new List<DateTime>(), Ties = new List<DateTime>() };
            foreach (var game in games)
            {
                if (MatchesBuildSequence(game, sequence))
                {
                    if (game.Result == (int)Result.Victory)
                    {
                        record.Wins.Add(game.DateTime);
                    }
                    else if (game.Result == (int)Result.Defeat)
                    {
                        record.Losses.Add(game.DateTime);
                    }
                    else if (game.Result == (int)Result.Tie)
                    {
                        record.Ties.Add(game.DateTime);
                    }
                }
            }
            return record;
        }

        private Record GetRecord(IEnumerable<Game> games)
        {
            var record = new Record { Wins = new List<DateTime>(), Losses = new List<DateTime>(), Ties = new List<DateTime>() };
            foreach (var game in games)
            {
                if (game.Result == (int)Result.Victory)
                {
                    record.Wins.Add(game.DateTime);
                }
                else if (game.Result == (int)Result.Defeat)
                {
                    record.Losses.Add(game.DateTime);
                }
                else if (game.Result == (int)Result.Tie)
                {
                    record.Ties.Add(game.DateTime);
                }
            }
            return record;
        }

        private bool MatchesBuildSequence(Game game, List<string> sequence)
        {
            if (game.Builds.First().Value != sequence.First())
            {
                return false;
            }

            var builds = game.Builds.Select(d => d.Value).ToList();
            for (int index = 0; index < sequence.Count() && index < builds.Count(); index++) // if a game doesn't get through the full sequence it can still be a match
            {
                if (builds[index] != sequence[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
