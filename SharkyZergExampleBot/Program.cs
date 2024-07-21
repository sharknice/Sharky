using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using SharkyZergExampleBot;
using System;

Console.WriteLine("Starting SharkyZergExampleBot");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

var zergBuildChoices = new ZergBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Zerg] = zergBuildChoices.BuildChoices;

// Enable zerg specific tasks by default.  These can also be enabled or disabled for specific builds in the OnFrame or StartBuild methods
defaultSharkyBot.MicroTaskData[typeof(BurrowBlockExpansionsTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(BurrowDronesFromHarras).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(CreepTumorTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(ChangelingScoutTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenCreepTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenDefendTask).Name].Enable();
defaultSharkyBot.MicroTaskData[typeof(QueenInjectTask).Name].Enable();

var sharkyExampleBot = defaultSharkyBot.CreateBot();

var myRace = Race.Zerg;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(sharkyExampleBot, @"Gresvan513AIE.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.RandomBuild).Wait();
}
else
{
    gameConnection.RunLadder(sharkyExampleBot, myRace, args).Wait();
}
