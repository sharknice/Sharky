using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using SharkyProtossExampleBot;
using System;

Console.WriteLine("Starting SharkyProtossExampleBot");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

var protossBuildChoices = new ProtossBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Protoss] = protossBuildChoices.BuildChoices;
defaultSharkyBot.SharkyOptions.ControlCamera = true;

var sharkyExampleBot = defaultSharkyBot.CreateBot();

var myRace = Race.Protoss;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(sharkyExampleBot, @"Gresvan513AIE.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.RandomBuild).Wait();
}
else
{
    gameConnection.RunLadder(sharkyExampleBot, myRace, args).Wait();
}
