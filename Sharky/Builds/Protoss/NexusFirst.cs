using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class NexusFirst : ProtossSharkyBuild
    {
       ICounterTransitioner ProtossCounterTransitioner;

        public NexusFirst(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, ChronoData nexusManager, ICounterTransitioner protossCounterTransitioner, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, nexusManager, protossCounterTransitioner, unitCountService)
        {
            ProtossCounterTransitioner = protossCounterTransitioner;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
            };

            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed>= 17)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) >= 2)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 1)
                {
                    MacroData.DesiredGases = 1;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_ASSIMILATOR) >= 1)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) > 1 && UnitCountService.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0;
        }

        public override List<string> CounterTransition(int frame)
        {
            return ProtossCounterTransitioner.DefaultCounterTransition(frame);
        }
    }
}
