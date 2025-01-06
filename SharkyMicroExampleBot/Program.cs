using Sharky;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Attack;
using Sharky.MicroTasks;
using SC2APIProtocol;
using Sharky.MicroControllers;
using Sharky.Builds;
using Sharky.Managers;

Console.WriteLine("Hello, Micro AI Arena!");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

defaultSharkyBot.TargetingService = new MicroTargetingService(defaultSharkyBot);
defaultSharkyBot.MicroController = new AdvancedMicroController(defaultSharkyBot);
var advancedAttackTask = new AdvancedAttackTask(defaultSharkyBot, new EnemyCleanupService(defaultSharkyBot.MicroController, defaultSharkyBot.DamageService), new List<UnitTypes>(), 2f, true);
advancedAttackTask.ClaimAllUnits = true;
defaultSharkyBot.MicroTaskData[typeof(AttackTask).Name] = advancedAttackTask;
defaultSharkyBot.MicroTaskData[typeof(MiningTask).Name].Disable();
defaultSharkyBot.EnemyData.EnemyStrategies.Clear();
((MapManager)defaultSharkyBot.Managers.FirstOrDefault(m => m.GetType() == typeof(MapManager))).FullVisionMode = true;

var microBuild = new MicroBuild(defaultSharkyBot);
var protossBuilds = new Dictionary<string, ISharkyBuild> { [microBuild.Name()] = microBuild };
var microSequences = new List<List<string>> { new List<string> { microBuild.Name() } };
var microBuildSequences = new Dictionary<string, List<List<string>>>
{
    [Race.Terran.ToString()] = microSequences,
    [Race.Zerg.ToString()] = microSequences,
    [Race.Protoss.ToString()] = microSequences,
    [Race.Random.ToString()] = microSequences,
    ["Transition"] = microSequences
};
var microChoices = new BuildChoices { Builds = protossBuilds, BuildSequences = microBuildSequences };
defaultSharkyBot.BuildChoices[Race.Protoss] = microChoices;
defaultSharkyBot.BuildChoices[Race.Terran] = microChoices;
defaultSharkyBot.BuildChoices[Race.Zerg] = microChoices;

var bot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugService);
var myRace = Race.Random;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(bot, @"Tier2MicroAIArena_v4.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.Rush, realTime: false).Wait();
}
else
{
    gameConnection.RunLadder(bot, myRace, args).Wait();
}