namespace Sharky.Builds.BuildChoosing
{
    public class RandomBuildDecisionService : BuildDecisionService
    {
        protected BuildMatcher BuildMatcher;
        Random Random;

        public RandomBuildDecisionService(DefaultSharkyBot defaultSharkyBot) 
            : base(defaultSharkyBot) 
        { 
            BuildMatcher = defaultSharkyBot.BuildMatcher;
            Random = new Random();
        }

        public override List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace)
        {
            List<string> debugMessage = new List<string>();
            debugMessage.Add($"Choosing random build against {enemyBot.Name} - {enemyBot.Id} on {map}");
            Console.WriteLine($"Choosing random build against {enemyBot.Name} - {enemyBot.Id} on {map}");

            return buildSequences.Skip(Random.Next(0, buildSequences.Count - 1)).FirstOrDefault();
        }
    }
}
