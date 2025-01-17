using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using Sharky.Managers;
using Sharky.MicroTasks;
using Sharky.MicroTasks.Attack;
using SharkyMachineLearningExample.Builds;
using SharkyMachineLearningExample.Tasks;

Console.WriteLine("Hello Micro Learning");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

SetupLearningTask(defaultSharkyBot);
EnableMicroMode(defaultSharkyBot);
SetSharkyDebugOptions(defaultSharkyBot);

var sharkbot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugService);

var myRace = Race.Protoss;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(sharkbot, @"StalkerTrainingMillion.SC2Map", myRace, Race.Protoss, Difficulty.VeryHard, AIBuild.Rush, 0, realTime: false).Wait();
}
else
{
    gameConnection.RunLadder(sharkbot, myRace, args).Wait();
}

static void SetupLearningTask(DefaultSharkyBot defaultSharkyBot)
{
    var advancedAttackTask = new AdvancedAttackTask(defaultSharkyBot, new EnemyCleanupService(defaultSharkyBot.MicroController, defaultSharkyBot.DamageService), new List<UnitTypes>(), 2f, true);
    defaultSharkyBot.MicroTaskData[typeof(AttackTask).Name] = advancedAttackTask;
    var learningSubAttackTask = new LearningSubAttackTask(defaultSharkyBot, advancedAttackTask, 1.9f, false);
    advancedAttackTask.SubTasks.Add(typeof(LearningSubAttackTask).Name, learningSubAttackTask);
}

static void EnableMicroMode(DefaultSharkyBot defaultSharkyBot)
{
    defaultSharkyBot.MicroTaskData[typeof(MiningTask).Name].Disable();
    ((AdvancedAttackTask)defaultSharkyBot.MicroTaskData[typeof(AttackTask).Name]).ClaimAllUnits = true;
    defaultSharkyBot.EnemyData.EnemyStrategies.Clear();
    defaultSharkyBot.Managers.FirstOrDefault(m => m.GetType() == typeof(BuildManager)).NeverSkip = true;

    var buildChoices = new MicroBuildChoices(defaultSharkyBot);
    defaultSharkyBot.BuildChoices[Race.Protoss] = buildChoices.MicroChoices;
}

static void SetSharkyDebugOptions(DefaultSharkyBot defaultSharkyBot)
{
    defaultSharkyBot.SharkyOptions.TagOptions.TagsEnabled = true;
    defaultSharkyBot.SharkyOptions.TagOptions.TagTime = false;
    defaultSharkyBot.SharkyOptions.TagOptions.BuildTagsEnabled = false;
    defaultSharkyBot.SharkyOptions.TagOptions.TagsAllChat = false;
    defaultSharkyBot.SharkyOptions.ControlCamera = false;
    defaultSharkyBot.SharkyOptions.LogPerformance = false;
}