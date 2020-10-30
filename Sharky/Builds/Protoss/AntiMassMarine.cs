using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Protoss
{
    public class AntiMassMarine : SharkyBuild
    {
        public AntiMassMarine(BuildOptions buildOptions, MacroManager macro, UnitManager unitManager) : base(buildOptions, macro, unitManager)
        {

        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (Macro.FoodUsed >= 15)
            {
                if (Macro.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    Macro.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (Macro.FoodUsed >= 17 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (Macro.DesiredGases < 1)
                {
                    Macro.DesiredGases = 1;
                }
            }
            if (Macro.FoodUsed >= 18 && UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (Macro.DesiredGases < 2)
                {
                    Macro.DesiredGases = 2;
                }
            }
            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (Macro.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    Macro.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                //NexusAbilityManager.SaveEnergy = true;
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0 && UnitManager.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (Macro.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 3)
                {
                    Macro.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 3;
                }
            }

            if (UnitManager.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (Macro.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 20)
                {
                    Macro.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 20;
                }
                Macro.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
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

            if (Macro.FoodUsed >= 50)
            {
                if (Macro.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    Macro.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }
        }
    }
}
