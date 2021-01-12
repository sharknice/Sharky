using SC2APIProtocol;
using Sharky;
using Sharky.DefaultBot;
using System;

namespace SharkyExampleBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("starting");

            // first we need to create a game connection for the SC2 api. The bot uses this to communicate with the game
            var gameConnection = new GameConnection();

            // We get a default bot that has everything setup.  You can manually create one instead if you want to more heavily customize it.  
            var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

            // we configure the bot with our own builds
            defaultSharkyBot.BuildChoices[Race.Protoss] = MyBuildChoices.GetProtossBuildChoices(defaultSharkyBot);
            defaultSharkyBot.BuildChoices[Race.Zerg] = MyBuildChoices.GetZergBuildChoices(defaultSharkyBot);
            defaultSharkyBot.BuildChoices[Race.Terran] = MyBuildChoices.GetTerranBuildChoices(defaultSharkyBot);

            // we create a bot with the modified default bot we made
            var sharkyExampleBot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugService);

            var myRace = Race.Random;
            if (args.Length == 0)
            {
                // if there are no arguments passed we play against a comptuer opponent
                gameConnection.RunSinglePlayer(sharkyExampleBot, @"AutomatonLE.SC2Map", myRace, Race.Random, Difficulty.VeryHard, AIBuild.RandomBuild).Wait();
            }
            else
            {
                // when a bot runs on the ladder it will pass arguments for a specific map, enemy, etc.
                gameConnection.RunLadder(sharkyExampleBot, myRace, args).Wait();
            }
        }
    }
}
