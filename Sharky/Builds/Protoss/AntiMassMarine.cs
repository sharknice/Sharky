using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class AntiMassMarine : ProtossSharkyBuild
    {
        public AntiMassMarine(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager) : base(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;

            NexusManager.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            NexusManager.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
                UnitTypes.PROTOSS_STALKER
            };

            AttackData.ArmyFoodAttack = 5;
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
            if (MacroData.FoodUsed >= 17 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 1)
                {
                    MacroData.DesiredGases = 1;
                }
            }
            if (MacroData.FoodUsed >= 18 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 2)
                {
                    MacroData.DesiredGases = 2;
                }
                BuildOptions.StrictGasCount = false;
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                NexusManager.ChronodUnits.Remove(UnitTypes.PROTOSS_PROBE);
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0 && UnitManager.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 3)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 3;
                }
            }

            if (UnitManager.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 20)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 20;
                }
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_STALKER) > 0)
            {
                BuildOptions.ProtossBuildOptions.PylonsAtDefensivePoint = 1;
                BuildOptions.ProtossBuildOptions.ShieldsAtDefensivePoint = 1;
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_STALKER) >= 2)
            {
                BuildOptions.ProtossBuildOptions.ShieldsAtDefensivePoint = 2;
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_STALKER) >= 3)
            {
                BuildOptions.ProtossBuildOptions.ShieldsAtDefensivePoint = 3;
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_STALKER) >= 4)
            {
                BuildOptions.ProtossBuildOptions.PylonsAtDefensivePoint = 2;
                BuildOptions.ProtossBuildOptions.ShieldsAtDefensivePoint = 4;
            }

            if (MacroData.FoodUsed >= 50)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }
        }

        public override bool Transition()
        {
            return MacroData.FoodUsed > 50 && UnitManager.Count(UnitTypes.PROTOSS_NEXUS) > 1;
        }
    }
}
