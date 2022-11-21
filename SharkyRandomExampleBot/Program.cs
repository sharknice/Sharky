using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using SharkyProtossExampleBot;
using SharkyTerranExampleBot;
using SharkyZergExampleBot;
using System;

Console.WriteLine("Starting SharkyRandomExampleBot");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

var protossBuildChoices = new ProtossBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Protoss] = protossBuildChoices.BuildChoices;

var terranBuildChoices = new TerranBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Terran] = terranBuildChoices.BuildChoices;

var zergBuildChoices = new ZergBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Zerg] = zergBuildChoices.BuildChoices;

defaultSharkyBot.MicroTaskData[typeof(BurrowBlockExpansionsTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(BurrowDronesFromHarras).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(CreepTumorTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(ChangelingScoutTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenCreepTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenDefendTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenInjectTask).Name].Enable();

var sharkyExampleBot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugService);

var myRace = Race.Random;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(sharkyExampleBot, @"GlitteringAshesAIE.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.RandomBuild).Wait();
}
else
{
    gameConnection.RunLadder(sharkyExampleBot, myRace, args).Wait();
}

