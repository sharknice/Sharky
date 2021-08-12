using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class NexusFirst : ProtossSharkyBuild
    {
        public NexusFirst(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
        }

        public NexusFirst(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService, MicroTaskData microTaskData, FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, chronoData, counterTransitioner, unitCountService, microTaskData, frameToTimeConverter)
        {
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
            if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
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
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
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
    }
}
