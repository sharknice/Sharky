using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Protoss;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using SharkyExampleBot.Builds;
using System;
using System.Collections.Generic;

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

            // we configure the bot with our own protoss builds
            defaultSharkyBot.BuildChoices[Race.Protoss] = GetProtossBuildChoices(defaultSharkyBot);

            // we create a bot with the modified default bot we made
            var sharkyExampleBot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugManager);

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

        static BuildChoices GetProtossBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            // we can use this to switch builds mid-game if we detect certain strategies
            var protossCounterTransitioner = new ProtossCounterTransitioner(defaultSharkyBot.EnemyStrategyManager, defaultSharkyBot.SharkyOptions);

            // a probe microcontroller for our proxy builds
            var probeMicroController = new IndividualMicroController(defaultSharkyBot.MapDataService, defaultSharkyBot.UnitDataManager, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.DebugManager, defaultSharkyBot.SharkyPathFinder, defaultSharkyBot.BaseManager, defaultSharkyBot.SharkyOptions, defaultSharkyBot.DamageService, MicroPriority.JustLive, false);

            // We create all of our builds
            var proxyVoidRay = new ProxyVoidRay(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.ChronoData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.MicroManager, protossCounterTransitioner, defaultSharkyBot.UnitDataManager, defaultSharkyBot.ProxyLocationService, defaultSharkyBot.DebugManager, defaultSharkyBot.UnitCountService, probeMicroController);
            var zealotRush = new ZealotRush(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.ChronoData, protossCounterTransitioner, defaultSharkyBot.MicroManager, defaultSharkyBot.UnitCountService);
            var robo = new Robo(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.ChronoData, defaultSharkyBot.EnemyRaceManager, defaultSharkyBot.MicroManager, protossCounterTransitioner, defaultSharkyBot.UnitCountService);
            var nexusFirst = new NexusFirst(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.ChronoData, protossCounterTransitioner, defaultSharkyBot.UnitCountService);
            var protossRobo = new ProtossRobo(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.ChronoData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.MicroManager, defaultSharkyBot.EnemyRaceManager, protossCounterTransitioner, defaultSharkyBot.UnitCountService);

            var buildNothing = new BuildNothing(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.UnitCountService);

            // We add all the builds to a build dictionary which we will later pass to the BuildChoices. 
            var protossBuilds = new Dictionary<string, ISharkyBuild>
            {
                [buildNothing.Name()] = buildNothing,
                [proxyVoidRay.Name()] = proxyVoidRay,
                [zealotRush.Name()] = zealotRush,
                [robo.Name()] = robo,
                [nexusFirst.Name()] = nexusFirst,
                [protossRobo.Name()] = protossRobo,
            };

            // we create build sequences to be used by each matchup
            var defaultSequences = new List<List<string>>
            {
                //new List<string> { buildNothing.Name() },
                new List<string> { nexusFirst.Name(), robo.Name(), protossRobo.Name() },
                new List<string> { proxyVoidRay.Name() }
            };
            var zergSequences = new List<List<string>>
            {
                new List<string> { zealotRush.Name() },
                new List<string> { proxyVoidRay.Name() }
            };
            var transitionSequences = new List<List<string>>
            {
                new List<string> { robo.Name(), protossRobo.Name() }
            };
            var protossBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = defaultSequences,
                [Race.Zerg.ToString()] = zergSequences,
                [Race.Protoss.ToString()] = defaultSequences,
                [Race.Random.ToString()] = defaultSequences,
                ["Transition"] = transitionSequences
            };

            return new BuildChoices { Builds = protossBuilds, BuildSequences = protossBuildSequences };
        }
    }
}
