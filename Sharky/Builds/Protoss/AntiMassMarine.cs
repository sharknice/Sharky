using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Protoss
{
    public class AntiMassMarine : SharkyBuild
    {
        public AntiMassMarine(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager) : base(buildOptions, macroData, unitManager)
        {

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
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                //NexusAbilityManager.SaveEnergy = true;
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
                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
                //NexusAbilityManager.PriotitizedAbilities.Add(UpgradeType.LookUp[Upgrades.WARPGATERESEARCH].Ability);
                //if (shark.Observation.Observation.RawData.Player.UpgradeIds.Contains(Upgrades.WARPGATERESEARCH))
                //{
                //    shark.NexusAbilityManager.SaveEnergy = false;
                //}
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
    }
}
