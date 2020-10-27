using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Managers
{
    public class BuildManager : SharkyManager
    {
        // do what build.cs and sharkbuild.cs does, has a SharkyBuild, switches builds, builds the actual units, or calls something to build the actual units

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            return new List<SC2APIProtocol.Action>();
        }
    }
}
