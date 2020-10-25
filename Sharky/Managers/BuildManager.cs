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
            if (sharkBuild.Count(UnitTypes.NEXUS) == 0)
            {
                BuildProduction(shark, sharkBuild);
            }

            if (sharkBuild.Count(UnitTypes.PROBE) == 0)
            {
                BuildUnits(shark, sharkBuild);
            }

            // TODO: change pylonsinmineralline etc. to only build when you need a pylon anyways, unless toggle is off for it
            CannonsInMineralLine(shark, sharkBuild);
            CannonsAtProxy(shark, sharkBuild);
            CannonAtEveryBase(shark, sharkBuild);
            ShieldsAtExpansions(shark, sharkBuild);

            ConstructAdditionalPylons(shark, sharkBuild);
            BuildGas(shark, sharkBuild);
            BuildProduction(shark, sharkBuild);

            if (ResearchUpgrades(shark, sharkBuild) && (sharkBuild.Minerals() < 250 || sharkBuild.Gas() < 250))
            {
                //return;
            }

            BuildTech(shark, sharkBuild);
            BuildUnits(shark, sharkBuild);


            return new List<SC2APIProtocol.Action>();
        }
    }
}
