using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public abstract class SharkyBuild : ISharkyBuild
    {
        protected BuildOptions BuildOptions;
        protected MacroData MacroData;
        protected UnitManager UnitManager;
        protected AttackData AttackData;
        protected IChatManager ChatManager;

        public SharkyBuild(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager)
        {
            BuildOptions = buildOptions;
            MacroData = macroData;
            UnitManager = unitManager;
            AttackData = attackData;
            ChatManager = chatManager;
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

            AttackData.CustomAttackFunction = false;
            BuildOptions.StrictGasCount = false;
            BuildOptions.StrictSupplyCount = false;
            BuildOptions.StrictWorkerCount = false;

            foreach (var u in MacroData.Units)
            {
                MacroData.DesiredUnitCounts[u] = 0;
            }
            foreach (var u in MacroData.Production)
            {
                MacroData.DesiredProductionCounts[u] = 0;
            }

            if (MacroData.Race == SC2APIProtocol.Race.Protoss)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
            }
            else if (MacroData.Race == SC2APIProtocol.Race.Terran)
            {
                MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] = 1;
            }
            else if (MacroData.Race == SC2APIProtocol.Race.Zerg)
            {
                MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] = 1;
            }
        }

        public virtual bool Transition(int frame)
        {
            return false;
        }

        public virtual List<string> CounterTransition(int frame)
        {
            return null;
        }
    }
}
