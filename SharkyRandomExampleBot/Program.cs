using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
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

