using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.Zerg;
using Sharky.DefaultBot;
using SharkyZergExampleBot.Builds;
using SharkyZergExampleBot.MicroTasks;
using System.Collections.Generic;

namespace SharkyZergExampleBot
{
    public class ZergBuildChoices
    {
        public BuildChoices BuildChoices { get; private set; }

        public ZergBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            var zerglingRush = new BasicZerglingRush(defaultSharkyBot);
            var mutaliskRush = new MutaliskRush(defaultSharkyBot);

            var builds = new Dictionary<string, ISharkyBuild>
            {
                [zerglingRush.Name()] = zerglingRush,
                [mutaliskRush.Name()] = mutaliskRush
            };

            var versusEverything = new List<List<string>>
            {
                new List<string> { zerglingRush.Name(), mutaliskRush.Name() },
                new List<string> { mutaliskRush.Name() },
            };

            var transitions = new List<List<string>>
            {
                new List<string> { mutaliskRush.Name() },
            };

            var buildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = versusEverything,
                [Race.Zerg.ToString()] = versusEverything,
                [Race.Protoss.ToString()] = versusEverything,
                [Race.Random.ToString()] = versusEverything,
                ["Transition"] = transitions,
            };

            BuildChoices = new BuildChoices { Builds = builds, BuildSequences = buildSequences };

            AddZergTasks(defaultSharkyBot);
        }

        void AddZergTasks(DefaultSharkyBot defaultSharkyBot)
        {
            var overlordScoutTask = new OverlordScoutTask(defaultSharkyBot, 2, true);
            defaultSharkyBot.MicroTaskData.MicroTasks[overlordScoutTask.GetType().Name] = overlordScoutTask;
        }
    }
}
