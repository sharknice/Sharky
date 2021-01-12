using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Protoss;
using Sharky.Builds.Terran;
using Sharky.Builds.Zerg;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using SharkyExampleBot.Builds;
using System.Collections.Generic;

namespace SharkyExampleBot
{
    public static class MyBuildChoices
    {
        public static BuildChoices GetProtossBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            // we can use this to switch builds mid-game if we detect certain strategies (it can also be handled specificly for each build)
            var protossCounterTransitioner = new ProtossCounterTransitioner(defaultSharkyBot.EnemyData, defaultSharkyBot.SharkyOptions);

            // a probe microcontroller for our proxy builds
            var probeMicroController = new IndividualMicroController(defaultSharkyBot.MapDataService, defaultSharkyBot.SharkyUnitData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.DebugService, defaultSharkyBot.SharkyPathFinder, defaultSharkyBot.BaseData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.DamageService, defaultSharkyBot.UnitDataService, MicroPriority.JustLive, false);

            // We create all of our builds
            var proxyVoidRay = new ProxyVoidRay(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.ChronoData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.MicroTaskData, protossCounterTransitioner, defaultSharkyBot.SharkyUnitData, defaultSharkyBot.ProxyLocationService, defaultSharkyBot.DebugService, defaultSharkyBot.UnitCountService, probeMicroController);
            var zealotRush = new ZealotRush(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.ChronoData, protossCounterTransitioner, defaultSharkyBot.UnitCountService);
            var robo = new Robo(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.ChronoData, defaultSharkyBot.EnemyData, defaultSharkyBot.MicroTaskData, protossCounterTransitioner, defaultSharkyBot.UnitCountService);
            var nexusFirst = new NexusFirst(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.ChronoData, protossCounterTransitioner, defaultSharkyBot.UnitCountService);
            var protossRobo = new ProtossRobo(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.ChronoData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.MicroTaskData, defaultSharkyBot.EnemyData, protossCounterTransitioner, defaultSharkyBot.UnitCountService);

            // We add all the builds to a build dictionary which we will later pass to the BuildChoices. 
            var protossBuilds = new Dictionary<string, ISharkyBuild>
            {
                [proxyVoidRay.Name()] = proxyVoidRay,
                [zealotRush.Name()] = zealotRush,
                [robo.Name()] = robo,
                [nexusFirst.Name()] = nexusFirst,
                [protossRobo.Name()] = protossRobo,
            };

            // we create build sequences to be used by each matchup
            var defaultSequences = new List<List<string>>
            {
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

        public static BuildChoices GetZergBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            var zerglingRush = new BasicZerglingRush(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.MicroTaskData, defaultSharkyBot.UnitCountService);
            var mutaliskRush = new MutaliskRush(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.UnitCountService);

            var zergBuilds = new Dictionary<string, ISharkyBuild>
            {
                [zerglingRush.Name()] = zerglingRush,
                [mutaliskRush.Name()] = mutaliskRush
            };

            var defaultSequences = new List<List<string>>
            {
                new List<string> { zerglingRush.Name(), mutaliskRush.Name() },
                new List<string> { mutaliskRush.Name() },
            };

            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = defaultSequences,
                [Race.Zerg.ToString()] = defaultSequences,
                [Race.Protoss.ToString()] = defaultSequences,
                [Race.Random.ToString()] = defaultSequences,
                ["Transition"] = defaultSequences
            };

            return new BuildChoices { Builds = zergBuilds, BuildSequences = buildSequences };
        }

        public static BuildChoices GetTerranBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            var scveMicroController = new IndividualMicroController(defaultSharkyBot.MapDataService, defaultSharkyBot.SharkyUnitData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.DebugService, defaultSharkyBot.SharkyPathFinder, defaultSharkyBot.BaseData, defaultSharkyBot.SharkyOptions, defaultSharkyBot.DamageService, defaultSharkyBot.UnitDataService, MicroPriority.JustLive, false);

            var massMarines = new MassMarines(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.UnitCountService);
            var bansheesAndMarines = new BansheesAndMarines(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.MicroTaskData, defaultSharkyBot.UnitCountService);
            var reaperCheese = new ReaperCheese(defaultSharkyBot.BuildOptions, defaultSharkyBot.MacroData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.AttackData, defaultSharkyBot.ChatService, defaultSharkyBot.MicroTaskData, defaultSharkyBot.UnitCountService, defaultSharkyBot.SharkyUnitData, defaultSharkyBot.ProxyLocationService, defaultSharkyBot.DebugService, scveMicroController);

            var builds = new Dictionary<string, ISharkyBuild>
            {
                [reaperCheese.Name()] = reaperCheese,
                [massMarines.Name()] = massMarines,
                [bansheesAndMarines.Name()] = bansheesAndMarines
            };

            var defaultSequences = new List<List<string>>
            {
                new List<string> { reaperCheese.Name(), bansheesAndMarines.Name() },
                new List<string> { massMarines.Name(), bansheesAndMarines.Name() },
                new List<string> { bansheesAndMarines.Name() }
            };

            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = defaultSequences,
                [Race.Zerg.ToString()] = defaultSequences,
                [Race.Protoss.ToString()] = defaultSequences,
                [Race.Random.ToString()] = defaultSequences,
                ["Transition"] = defaultSequences
            };

            return new BuildChoices { Builds = builds, BuildSequences = buildSequences };
        }
    }
}
