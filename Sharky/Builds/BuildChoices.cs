namespace Sharky.Builds
{
    public class BuildChoices
    {
        public Dictionary<string, ISharkyBuild> Builds { get; set; }
        public Dictionary<string, List<List<string>>> BuildSequences { get; set; }

        public override string ToString()
        {
            return $"Builds: {string.Join(", ", Builds.Keys)}, sequences: {string.Join(", ", BuildSequences.Keys)}";
        }
    }
}
