using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.Protoss
{
    public class NexusFirstWithForge : ProtossSharkyBuild
    {
        BaseData BaseData;

        public NexusFirstWithForge(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
            BaseData = defaultSharkyBot.BaseData;
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

            TargetingData.WallOffBasePosition = BuildingPlacement.WallOffBasePosition.Natural;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            SendProbeToNaturalForFirstPylon(frame);
            SendProbeForNexus(frame);
            ScoutThird(frame);
            SendProbeForGateway(frame);

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
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 1;
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

        protected void ScoutThird(int frame)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) == 2 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) == 0 && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && u.Value.Unit.BuildProgress < .1f))
            {
                var third = BaseData.BaseLocations.Skip(2).FirstOrDefault();
                if (third != null)
                {
                    PrePositionBuilderTask.SendBuilder(third.MineralLineLocation, frame);
                }
            }
        }

        protected void SendProbeForGateway(int frame)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) == 2 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) == 0 && MacroData.Minerals > 150)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }
    }
}
