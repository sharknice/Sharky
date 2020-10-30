using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class SharkyBuild : ISharkyBuild
    {
        protected BuildOptions BuildOptions;
        protected MacroManager Macro;
        protected UnitManager UnitManager;

        public SharkyBuild(BuildOptions buildOptions, MacroManager macro, UnitManager unitManager)
        {
            BuildOptions = buildOptions;
            Macro = macro;
            UnitManager = unitManager;
        }

        public virtual List<string> CounterTransition()
        {
            return null;
        }

        public string Name()
        {
            return GetType().Name;
        }

        public virtual void OnFrame(ResponseObservation observation)
        {

        }

        public virtual void StartBuild(int frame)
        {
            Console.WriteLine($"{frame} Build: {Name()}");

            BuildOptions.StrictGasCount = false;
            BuildOptions.StrictSupplyCount = false;
            BuildOptions.StrictWorkerCount = false;

            foreach (var u in Macro.Units)
            {
                Macro.DesiredUnitCounts[u] = 0;
            }
            foreach (var u in Macro.Production)
            {
                Macro.DesiredProductionCounts[u] = 0;
            }

            if (Macro.Race == SC2APIProtocol.Race.Protoss)
            {
                Macro.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
            }
            else if (Macro.Race == SC2APIProtocol.Race.Protoss)
            {
                Macro.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] = 1;
            }
        }

        public virtual bool Transition()
        {
            return false;
        }
    }
}
