using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.BuildChoosing
{
    public class BuildMatcher
    {
        public bool MatchesBuildSequence(Game game, IEnumerable<string> sequence)
        {
            if (game.Builds.First().Value != sequence.First())
            {
                return false;
            }

            var builds = game.Builds.Select(d => d.Value).ToList();
            for (int index = 0; index < sequence.Count() && index < builds.Count() && index < 3; index++) // if a game doesn't get through the full sequence it can still be a match, if it matches the first 3 we count it as a full match because of counter builds etc.  
            {
                if (builds[index] != sequence.ElementAt(index))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
