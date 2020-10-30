using Sharky.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds
{
    public class MacroBalancer : IMacroBalancer
    {
        BuildOptions BuildOptions;
        UnitManager UnitManager;
        MacroManager MacroManager;
        UnitDataManager UnitDataManager;

        public MacroBalancer(BuildOptions buildOptions, UnitManager unitManager, MacroManager macroManager, UnitDataManager unitDataManager)
        {
            BuildOptions = buildOptions;
            UnitManager = unitManager;
            MacroManager = macroManager;
            UnitDataManager = unitDataManager;
        }

        public void BalanceSupply()
        {
            BalancePylons();
        }

        void BalancePylons()
        {
            if (!BuildOptions.StrictSupplyCount)
            {
                var productionCapacity = UnitManager.Count(UnitTypes.PROTOSS_NEXUS) + (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) * 2) + (UnitManager.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) * 6) + (UnitManager.Count(UnitTypes.PROTOSS_STARGATE) * 6);
                MacroManager.DesiredPylons = (int)Math.Ceiling(((MacroManager.FoodUsed - (UnitManager.Count(UnitTypes.PROTOSS_NEXUS) * 12)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroManager.BuildPylon = MacroManager.DesiredPylons > UnitManager.Count(UnitTypes.PROTOSS_PYLON) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON));
        }

        public void BalanceGases()
        {
            if (!BuildOptions.StrictGasCount)
            {
                MacroManager.DesiredGases = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress > .7) * 2;
            }

            MacroManager.BuildGas = MacroManager.DesiredGases > UnitManager.Count(UnitTypes.PROTOSS_ASSIMILATOR) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR));
        }

        public void BalanceTech()
        {
            foreach (var u in MacroManager.Tech)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroManager.BuildTech[u] = UnitManager.Count(u) < MacroManager.DesiredTechCounts[u] + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            }
        }

        public void BalanceProductionBuildings()
        {
            foreach (var u in MacroManager.Production)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroManager.BuildProduction[u] = UnitManager.Count(u) < MacroManager.DesiredProductionCounts[u] + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            }
        }

        public void BalanceProduction()
        {
            BalanceProduction(MacroManager.Units);

            var nexuss = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var gateways = UnitManager.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var robos = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ROBOTICSFACILITY &&c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var stargates = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STARGATE && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();

            var unitTypes = new List<UnitTypes>();
            for (int index = 0; index < nexuss; index++)
            {
                unitTypes.AddRange(MacroManager.NexusUnits);
            }
            for (int index = 0; index < gateways; index++)
            {
                unitTypes.AddRange(MacroManager.GatewayUnits);
            }
            for (int index = 0; index < robos; index++)
            {
                unitTypes.AddRange(MacroManager.RoboticsFacilityUnits);
            }
            for (int index = 0; index < stargates; index++)
            {
                unitTypes.AddRange(MacroManager.StargateUnits);
            }

            BalanceProduction(unitTypes);

            if (MacroManager.NexusUnits.Where(u => MacroManager.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroManager.NexusUnits);
            }
            if (MacroManager.GatewayUnits.Where(u => MacroManager.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroManager.GatewayUnits);
            }
            if (MacroManager.RoboticsFacilityUnits.Where(u => MacroManager.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroManager.RoboticsFacilityUnits);
            }
            if (MacroManager.StargateUnits.Where(u => MacroManager.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroManager.StargateUnits);
            }

            if (!BuildOptions.StrictWorkerCount)
            {
                var nexuses = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS);
                var completedNexuses = nexuses.Where(n => n.UnitCalculation.Unit.BuildProgress == 1);
                var buildingNexusCount = nexuses.Count(n => n.UnitCalculation.Unit.BuildProgress < 1);
                var desiredProbes = completedNexuses.Sum(n => n.UnitCalculation.Unit.IdealHarvesters + 4) + (buildingNexusCount * 22) + 1; // +4 because 2 are inside the gases and you can't see them

                if (UnitManager.Count(UnitTypes.PROTOSS_PROBE) < desiredProbes && UnitManager.Count(UnitTypes.PROTOSS_PROBE) < 70)
                {
                    MacroManager.BuildUnits[UnitTypes.PROTOSS_PROBE] = true;
                }
                else
                {
                    MacroManager.BuildUnits[UnitTypes.PROTOSS_PROBE] = false;
                }
            }
        }

        public void BalanceProduction(List<UnitTypes> unitTypes)
        {
            var desiredTotal = unitTypes.Sum(u => MacroManager.DesiredUnitCounts[u]);
            var currentTotal = unitTypes.Sum(u => UnitManager.Count(u));
            var numberOfTypes = unitTypes.Count();

            foreach (var u in unitTypes)
            {
                var desiredRatio = MacroManager.DesiredUnitCounts[u] / (double)desiredTotal;

                var count = UnitManager.Count(u);
                if (u == UnitTypes.PROTOSS_WARPPRISM) { count += UnitManager.Count(UnitTypes.PROTOSS_WARPPRISMPHASING); }

                var actualRatio = count / (double)currentTotal;

                if (MacroManager.DesiredUnitCounts[u] == 1 && count == 0)
                {
                    MacroManager.BuildUnits[u] = true;
                }
                else if (actualRatio > desiredRatio || count >= MacroManager.DesiredUnitCounts[u])
                {
                    if (MacroManager.DesiredUnitCounts[u] < MacroManager.DesiredUnitCounts[u] && MacroManager.Minerals > 350 && UnitDataManager.UnitData[u].VespeneCost == 0)
                    {
                        MacroManager.BuildUnits[u] = true;
                    }
                    else
                    {
                        MacroManager.BuildUnits[u] = false;
                    }
                }
                else
                {
                    MacroManager.BuildUnits[u] = true;
                }
            }
        }

        public void BalanceGasWorkers()
        {

        }
    }
}
