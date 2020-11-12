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
        MacroData MacroData;
        UnitDataManager UnitDataManager;

        public MacroBalancer(BuildOptions buildOptions, UnitManager unitManager, MacroData macroData, UnitDataManager unitDataManager)
        {
            BuildOptions = buildOptions;
            UnitManager = unitManager;
            MacroData = macroData;
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
                MacroData.DesiredPylons = (int)Math.Ceiling(((MacroData.FoodUsed - (UnitManager.Count(UnitTypes.PROTOSS_NEXUS) * 12)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroData.BuildPylon = MacroData.DesiredPylons > UnitManager.Count(UnitTypes.PROTOSS_PYLON) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON));
        }

        public void BalanceGases()
        {
            if (!BuildOptions.StrictGasCount)
            {
                MacroData.DesiredGases = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress > .7) * 2;
            }

            MacroData.BuildGas = MacroData.DesiredGases > UnitManager.Count(UnitTypes.PROTOSS_ASSIMILATOR) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR));
        }

        public void BalanceTech()
        {
            foreach (var u in MacroData.Tech)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroData.BuildTech[u] = UnitManager.Count(u) < MacroData.DesiredTechCounts[u];
                if (MacroData.BuildTech[u] && UnitManager.Commanders.Values.Any(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)))
                {
                    MacroData.BuildTech[u] = false;
                }
            }
        }

        public void BalanceProductionBuildings()
        {
            foreach (var u in MacroData.Production)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroData.BuildProduction[u] = UnitManager.EquivalentTypeCount(u) < MacroData.DesiredProductionCounts[u];
                if (MacroData.BuildProduction[u] && UnitManager.Commanders.Values.Any(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)))
                {
                    MacroData.BuildProduction[u] = false;
                }

            }
        }

        public void BalanceProduction()
        {
            BalanceProduction(MacroData.Units);

            var nexuss = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var gateways = UnitManager.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var robos = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ROBOTICSFACILITY &&c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var stargates = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STARGATE && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();

            var unitTypes = new List<UnitTypes>();
            for (int index = 0; index < nexuss; index++)
            {
                unitTypes.AddRange(MacroData.NexusUnits);
            }
            for (int index = 0; index < gateways; index++)
            {
                unitTypes.AddRange(MacroData.GatewayUnits);
            }
            for (int index = 0; index < robos; index++)
            {
                unitTypes.AddRange(MacroData.RoboticsFacilityUnits);
            }
            for (int index = 0; index < stargates; index++)
            {
                unitTypes.AddRange(MacroData.StargateUnits);
            }

            BalanceProduction(unitTypes);

            if (MacroData.NexusUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.NexusUnits);
            }
            if (MacroData.GatewayUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.GatewayUnits);
            }
            if (MacroData.RoboticsFacilityUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.RoboticsFacilityUnits);
            }
            if (MacroData.StargateUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.StargateUnits);
            }

            if (!BuildOptions.StrictWorkerCount)
            {
                var resourceCenters = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                var completedResourceCenters = resourceCenters.Where(n => n.UnitCalculation.Unit.BuildProgress == 1);
                var buildingResourceCentersCount = resourceCenters.Count(n => n.UnitCalculation.Unit.BuildProgress < 1);
                var desiredWorkers = completedResourceCenters.Sum(n => n.UnitCalculation.Unit.IdealHarvesters + 4) + (buildingResourceCentersCount * 22) + 1; // +4 because 2 are inside the gases and you can't see them

                if (UnitManager.Count(UnitTypes.PROTOSS_PROBE) < desiredWorkers && UnitManager.Count(UnitTypes.PROTOSS_PROBE) < 70)
                {
                    MacroData.BuildUnits[UnitTypes.PROTOSS_PROBE] = true;
                }
                else
                {
                    MacroData.BuildUnits[UnitTypes.PROTOSS_PROBE] = false;
                }
            }
        }

        public void BalanceProduction(List<UnitTypes> unitTypes)
        {
            var desiredTotal = unitTypes.Sum(u => MacroData.DesiredUnitCounts[u]);
            var currentTotal = unitTypes.Sum(u => UnitManager.Count(u));
            var numberOfTypes = unitTypes.Count();

            foreach (var u in unitTypes)
            {
                var desiredRatio = MacroData.DesiredUnitCounts[u] / (double)desiredTotal;

                var count = UnitManager.Count(u);
                if (u == UnitTypes.PROTOSS_ARCHON)
                {
                    continue;
                }

                var trainingData = UnitDataManager.TrainingData[u];
                count += UnitManager.SelfUnits.Count(u => trainingData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)trainingData.Ability));
                if (u == UnitTypes.PROTOSS_WARPPRISM) { count += UnitManager.Count(UnitTypes.PROTOSS_WARPPRISMPHASING); }

                var actualRatio = count / (double)currentTotal;

                if (MacroData.DesiredUnitCounts[u] == 1 && count == 0)
                {
                    MacroData.BuildUnits[u] = true;
                }
                else if (actualRatio > desiredRatio || count >= MacroData.DesiredUnitCounts[u])
                {
                    if (MacroData.DesiredUnitCounts[u] < MacroData.DesiredUnitCounts[u] && MacroData.Minerals > 350 && UnitDataManager.UnitData[u].VespeneCost == 0) // TODO: this has got to be wrong
                    {
                        MacroData.BuildUnits[u] = true;
                    }
                    else
                    {
                        MacroData.BuildUnits[u] = false;
                    }
                }
                else
                {
                    MacroData.BuildUnits[u] = true;
                }
            }
        }

        public void BalanceGasWorkers()
        {

        }
    }
}
