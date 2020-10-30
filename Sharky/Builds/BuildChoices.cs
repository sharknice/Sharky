using System.Collections.Generic;

namespace Sharky.Builds
{
    public class BuildChoices
    {
        public Dictionary<string, ISharkyBuild> Builds { get; set; }
        public Dictionary<string, List<List<string>>> BuildSequences { get; set; }
    }
}
