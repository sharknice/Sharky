using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Protoss
{
    public class AntiMassMarine : SharkyBuild
    {
        public AntiMassMarine(BuildOptions buildOptions, MacroManager macroManager, UnitManager unitManager) : base(buildOptions, macroManager, unitManager)
        {

        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroManager.FoodUsed >= 15)
            {
                if (MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (MacroManager.FoodUsed >= 17 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroManager.DesiredGases < 1)
                {
                    MacroManager.DesiredGases = 1;
                }
            }
            if (MacroManager.FoodUsed >= 18 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroManager.DesiredGases < 2)
                {
                    MacroManager.DesiredGases = 2;
                }
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroManager.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroManager.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                //NexusAbilityManager.SaveEnergy = true;
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0 && UnitManager.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 3)
                {
                    MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 3;
                }
            }

            if (UnitManager.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroManager.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 20)
                {
                    MacroManager.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 20;
                }
                MacroManager.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
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

            if (MacroManager.FoodUsed >= 50)
            {
                if (MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    MacroManager.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }
        }
    }
}
