using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Protoss;
using Sharky.DefaultBot;
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
            var gameConnection = new GameConnection();

            var defaultSharkyBot = new DefaultSharkyBot(gameConnection);

            // TODO: make example roach rush build
            var proxyVoidRay = new ProxyVoidRay(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.UnitManager, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.NexusManager, defaultSharkyBot.SharkyOptions, defaultSharkyBot.MicroManager, defaultSharkyBot.EnemyRaceManager, defaultSharkyBot.ProtossCounterTransitioner, defaultSharkyBot.UnitDataManager, defaultSharkyBot.ProxyLocationService);
            var everyProtossUnit = new EveryProtossUnit(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.UnitManager, defaultSharkyBot.AttackData, defaultSharkyBot.ChatManager, defaultSharkyBot.NexusManager, defaultSharkyBot.ProtossCounterTransitioner);
            var protossBuilds = new Dictionary<string, ISharkyBuild>
            {
                [proxyVoidRay.Name()] = proxyVoidRay,
                [everyProtossUnit.Name()] = everyProtossUnit
            };
            var protosSequences = new List<List<string>>
            {
                new List<string> { proxyVoidRay.Name(), everyProtossUnit.Name() }
            };
            var protossBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = protosSequences,
                [Race.Zerg.ToString()] = protosSequences,
                [Race.Protoss.ToString()] = protosSequences,
                [Race.Random.ToString()] = protosSequences,
                ["Transition"] = protosSequences
            };

            defaultSharkyBot.BuildChoices[Race.Protoss] = new BuildChoices { Builds = protossBuilds, BuildSequences = protossBuildSequences };

            var sharkyBot = defaultSharkyBot.CreateBot(defaultSharkyBot.Managers, defaultSharkyBot.DebugManager);

            var myRace = Race.Protoss;
            if (args.Length == 0)
            {
                gameConnection.RunSinglePlayer(sharkyBot, @"AutomatonLE.SC2Map", myRace, Race.Random, Difficulty.VeryHard).Wait();
            }
            else
            {
                gameConnection.RunLadder(sharkyBot, myRace, args).Wait();
            }
        }
    }
}
