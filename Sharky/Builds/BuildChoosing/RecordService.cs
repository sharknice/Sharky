namespace Sharky.Builds.BuildChoosing
{
    public class RecordService
    {
        BuildMatcher BuildMatcher;

        public RecordService(BuildMatcher buildMatcher)
        {
            BuildMatcher = buildMatcher;
        }

        public Record GetSequenceRecord(IEnumerable<Game> games, List<string> sequence)
        {
            var record = new Record { Wins = new List<DateTime>(), Losses = new List<DateTime>(), Ties = new List<DateTime>() };
            foreach (var game in games)
            {
                if (BuildMatcher.MatchesBuildSequence(game, sequence))
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

        public Record GetRecord(IEnumerable<Game> games)
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
    }
}
