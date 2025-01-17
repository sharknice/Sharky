using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;

namespace SharkyMachineLearningExample.Builds
{
    public class MicroBuildChoices
    {
        public BuildChoices MicroChoices { get; private set; }

        public MicroBuildChoices(DefaultSharkyBot defaultSharkyBot)
        {
            var microLearningBuild = new MicroLearningBuild(defaultSharkyBot);

            var protossBuilds = new Dictionary<string, ISharkyBuild>
            {
                [microLearningBuild.Name()] = microLearningBuild
            };

            var microSequences = new List<List<string>>
            {
                new() { microLearningBuild.Name() },
            };

            var microBuildSequences = new Dictionary<string, List<List<string>>>
            {
                [Race.Terran.ToString()] = microSequences,
                [Race.Zerg.ToString()] = microSequences,
                [Race.Protoss.ToString()] = microSequences,
                [Race.Random.ToString()] = microSequences,
                ["Transition"] = microSequences
            };
            MicroChoices = new BuildChoices { Builds = protossBuilds, BuildSequences = microBuildSequences };
        }
    }
}
