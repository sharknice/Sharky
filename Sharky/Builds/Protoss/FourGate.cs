using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class FourGate : ProtossSharkyBuild
    {
        SharkyUnitData SharkyUnitData;

        bool OpeningAttackChatSent;

        public FourGate(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            OpeningAttackChatSent = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictWorkerCount = true;

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
            };

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PROBE] = 23;
            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (MacroData.FoodUsed >= 17 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 1)
                {
                    MacroData.DesiredGases = 1;
                }
            }
            if (MacroData.FoodUsed >= 20 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 2)
                {
                    MacroData.DesiredGases = 2;
                }
            }
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 1;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0 && UnitCountService.Count(UnitTypes.PROTOSS_ZEALOT) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 4)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 4;
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 4)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 4)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 4;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] = 1;
                }
            }
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 3)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 3;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 7)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 7;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] = 2;
                }
            }
            if (MacroData.FoodUsed >= 45)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 8)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 8;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 18)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 18;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] = 2;
                }
            }
            if (MacroData.FoodUsed >= 80)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }

            if (!OpeningAttackChatSent && MacroData.FoodArmy > 10)
            {
                ChatService.SendChatType("FourGate-FirstAttack");
                OpeningAttackChatSent = true;
            }
        }

        public override bool Transition(int frame)
        {
            return UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) > 1 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 4;
        }
    }
}
