using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using SharkyTerranExampleBot;
using System;

Console.WriteLine("Starting SharkyTerranExampleBot");

var gameConnection = new GameConnection();
var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

var terranBuildChoices = new TerranBuildChoices(defaultSharkyBot);
defaultSharkyBot.BuildChoices[Race.Terran] = terranBuildChoices.BuildChoices;

var sharkyExampleBot = defaultSharkyBot.CreateBot();

var myRace = Race.Terran;
if (args.Length == 0)
{
    gameConnection.RunSinglePlayer(sharkyExampleBot, @"AncientCisternAIE.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.RandomBuild).Wait();
}
else
{
    gameConnection.RunLadder(sharkyExampleBot, myRace, args).Wait();
}
