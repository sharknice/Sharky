using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using SharkyTerranExampleBot.Builds;
using System.Collections.Generic;

namespace SharkyTerranExampleBot
{
    public class TerranBuildChoices
    {
        public BuildChoices BuildChoices { get; private set; }

        public TerranBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            var hellionRush = new HellionRush(defaultSharkyBot);
            var massVikings = new MassVikings(defaultSharkyBot);
            var bansheesAndMarines = new BansheesAndMarines(defaultSharkyBot);
            var adaptiveOpening = new AdaptiveOpening(defaultSharkyBot);

            var scvMicroController = new IndividualMicroController(defaultSharkyBot, defaultSharkyBot.SharkyAdvancedPathFinder, MicroPriority.JustLive, false);
            var reaperCheese = new ReaperCheese(defaultSharkyBot, scvMicroController);

            var builds = new Dictionary<string, ISharkyBuild>
            {
                [hellionRush.Name()] = hellionRush,
                [massVikings.Name()] = massVikings,
                [bansheesAndMarines.Name()] = bansheesAndMarines,
                [adaptiveOpening.Name()] = adaptiveOpening,
                [reaperCheese.Name()] = reaperCheese,
            };

            var versusTerran = new List<List<string>>
            {
                new List<string> { hellionRush.Name() },
                new List<string> { reaperCheese.Name() }
            };
            var versusEverything = new List<List<string>>
            {
                new List<string> { adaptiveOpening.Name() },
                new List<string> { hellionRush.Name() },
                new List<string> { massVikings.Name() },

            };
            var transitions = new List<List<string>>
            {
                new List<string> { bansheesAndMarines.Name() },
            };

            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = versusTerran,
                [Race.Zerg.ToString()] = versusEverything,
                [Race.Protoss.ToString()] = versusEverything,
                [Race.Random.ToString()] = versusEverything,
                ["Transition"] = transitions,
            };

            BuildChoices = new BuildChoices { Builds = builds, BuildSequences = buildSequences };
        }
    }
}
