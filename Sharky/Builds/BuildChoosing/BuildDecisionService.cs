namespace Sharky.Builds.BuildChoosing
{
    public class BuildDecisionService : IBuildDecisionService
    {
        protected ChatService ChatService;
        protected EnemyPlayerService EnemyPlayerService;
        protected RecordService RecordService;

        public BuildDecisionService(DefaultSharkyBot defaultSharkyBot)
        {
            ChatService = defaultSharkyBot.ChatService;
            EnemyPlayerService = defaultSharkyBot.EnemyPlayerService;
            RecordService = defaultSharkyBot.RecordService;
        }

        protected bool BetterBuild(Record original, Record current)
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

        protected bool WonLastGame(Record record)
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

        public virtual List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            List<string> debugMessage = new List<string>();
            debugMessage.Add($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");
            Console.WriteLine($"Choosing build against {enemyBot.Name} - {enemyBot.Id} on {map}");

            var bestBuildSequence = buildSequences.First();

            var mapGames = enemyBot.Games.Where(g => g.MapName == map).Where(g => g.EnemyRace == enemyRace); // it is possible a bot could be updated and change races on the ladder
            Record bestRecord = null;

            var record = RecordService.GetRecord(mapGames);
            Console.WriteLine($"Same enemy, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"Same enemy, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (mapGames.Count() > 0)
            {
                // check games on this map
                foreach (var buildSequence in buildSequences)
                {
                    var buildRecord = RecordService.GetSequenceRecord(mapGames, buildSequence);
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            record = RecordService.GetRecord(enemyBot.Games.Where(g => g.EnemyRace == enemyRace));
            Console.WriteLine($"Same enemy, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"Same enemy, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            if (bestRecord == null || bestRecord.Wins.Count() == 0)
            {
                // check games on other maps
                foreach (var buildSequence in buildSequences)
                {
                    if (RecordService.GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                    var buildRecord = RecordService.GetSequenceRecord(enemyBot.Games.Where(g => g.EnemyRace == enemyRace), buildSequence);
                    Debug.WriteLine($"{string.Join(" ", buildSequence)} {buildRecord.Wins.Count()}-{buildRecord.Ties.Count()}-{buildRecord.Losses.Count()}");
                    if (BetterBuild(bestRecord, buildRecord))
                    {
                        bestBuildSequence = buildSequence;
                        bestRecord = buildRecord;
                        Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                    }
                }
            }

            record = RecordService.GetRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace).Where(g => g.MapName == map));
            Console.WriteLine($"All enemies, same race, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            debugMessage.Add($"All enemies, same race, same map: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
            
            if (!EnemyPlayerService.Tournament.Enabled) // use only the games for that specific bot for tournaments
            {
                if (bestRecord.Wins.Count() == 0)
                {
                    // check games on this map from other bots of the same race
                    foreach (var buildSequence in buildSequences)
                    {
                        if (RecordService.GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                        var buildRecord = RecordService.GetSequenceRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace).Where(g => g.MapName == map), buildSequence);
                        if (BetterBuild(bestRecord, buildRecord))
                        {
                            bestBuildSequence = buildSequence;
                            bestRecord = buildRecord;
                            Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                            debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        }
                    }
                }

                record = RecordService.GetRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace));
                Console.WriteLine($"All enemies, same race, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
                debugMessage.Add($"All enemies, same race, all maps: {record.Wins.Count()}-{record.Losses.Count()}-{record.Ties.Count()}");
                if (bestRecord.Wins.Count() == 0)
                {
                    // check games on other maps from other bots of the same race
                    foreach (var buildSequence in buildSequences)
                    {
                        if (RecordService.GetSequenceRecord(mapGames, buildSequence).Losses.Count() > 0) { continue; }
                        var buildRecord = RecordService.GetSequenceRecord(enemyBots.SelectMany(b => b.Games).Where(g => g.EnemyRace == enemyRace), buildSequence);
                        if (BetterBuild(bestRecord, buildRecord))
                        {
                            bestBuildSequence = buildSequence;
                            bestRecord = buildRecord;
                            Console.WriteLine($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                            debugMessage.Add($"choice: {string.Join(" ", bestBuildSequence)}, {bestRecord.Wins.Count()}-{bestRecord.Losses.Count()}-{bestRecord.Ties.Count()}");
                        }
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
    }
}
