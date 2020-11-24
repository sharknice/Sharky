using SC2APIProtocol;
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
            if (MacroData.Race == Race.Protoss)
            {
                BalancePylons();
            }
            else if (MacroData.Race == Race.Terran)
            {
                BalanceSupplyDepots();
            }
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

        void BalanceSupplyDepots()
        {
            if (!BuildOptions.StrictSupplyCount)
            {
                var productionCapacity = UnitManager.Count(UnitTypes.TERRAN_COMMANDCENTER) + UnitManager.Count(UnitTypes.TERRAN_ORBITALCOMMAND) + UnitManager.Count(UnitTypes.TERRAN_PLANETARYFORTRESS) + (UnitManager.Count(UnitTypes.TERRAN_BARRACKS) * 2) + (UnitManager.Count(UnitTypes.TERRAN_FACTORY) * 6) + (UnitManager.Count(UnitTypes.TERRAN_STARPORT) * 6);
                MacroData.DesiredSupplyDepots = (int)Math.Ceiling(((MacroData.FoodUsed - (UnitManager.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) * 12)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroData.BuildSupplyDepot = MacroData.DesiredSupplyDepots > UnitManager.EquivalentTypeCount(UnitTypes.TERRAN_SUPPLYDEPOT) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_SUPPLYDEPOT));
        }

        public void BalanceGases()
        {
            if (!BuildOptions.StrictGasCount)
            {
                MacroData.DesiredGases = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter) && c.UnitCalculation.Unit.BuildProgress > .7) * 2;
            }

            if (MacroData.Race == Race.Protoss)
            {
                MacroData.BuildGas = MacroData.DesiredGases > UnitManager.Count(UnitTypes.PROTOSS_ASSIMILATOR) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR));
            }
            else if (MacroData.Race == Race.Terran)
            {
                MacroData.BuildGas = MacroData.DesiredGases > UnitManager.Count(UnitTypes.TERRAN_REFINERY) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_REFINERY));
            }
            else if (MacroData.Race == Race.Zerg)
            {
                MacroData.BuildGas = MacroData.DesiredGases > UnitManager.Count(UnitTypes.ZERG_EXTRACTOR) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_EXTRACTOR));
            }
        }

        public void BalanceTech()
        {
            foreach (var u in MacroData.Tech)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroData.BuildTech[u] = UnitManager.Count(u) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredTechCounts[u];
            }
        }

        public void BalanceAddOns()
        {
            foreach (var u in MacroData.AddOns)
            {
                MacroData.BuildAddOns[u] = UnitManager.Count(u) < MacroData.DesiredAddOnCounts[u];
            }
        }

        public void BalanceProductionBuildings()
        {
            foreach (var u in MacroData.Production)
            {
                var unitData = UnitDataManager.BuildingData[u];
                MacroData.BuildProduction[u] = UnitManager.EquivalentTypeCount(u) + UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredProductionCounts[u];
            }
        }

        public void BalanceMorphs()
        {
            foreach (var u in MacroData.Morphs)
            {
                MacroData.Morph[u] = UnitManager.EquivalentTypeCount(u) < MacroData.DesiredMorphCounts[u];
            }
        }

        public void BalanceProduction()
        {
            BalanceProduction(MacroData.Units);

            if (MacroData.Race == Race.Protoss)
            {
                BalanceProtossProduction();
            }
            else if (MacroData.Race == Race.Terran)
            {
                BalanceTerranProduction();
            }

            if (!BuildOptions.StrictWorkerCount)
            {
                var resourceCenters = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                var completedResourceCenters = resourceCenters.Where(n => n.UnitCalculation.Unit.BuildProgress == 1);
                var buildingResourceCentersCount = resourceCenters.Count(n => n.UnitCalculation.Unit.BuildProgress < 1);
                var desiredWorkers = completedResourceCenters.Sum(n => n.UnitCalculation.Unit.IdealHarvesters + 4) + (buildingResourceCentersCount * 22) + 1; // +4 because 2 are inside the gases and you can't see them

                var workerType = UnitTypes.PROTOSS_PROBE;
                if (MacroData.Race == Race.Terran)
                {
                    workerType = UnitTypes.TERRAN_SCV;
                }
                else if (MacroData.Race == Race.Zerg)
                {
                    workerType = UnitTypes.ZERG_DRONE;
                }
                if (UnitManager.Count(workerType) < desiredWorkers && UnitManager.Count(workerType) < 70)
                {
                    MacroData.BuildUnits[workerType] = true;
                }
                else
                {
                    MacroData.BuildUnits[workerType] = false;
                }
            }
        }

        public void BalanceProtossProduction()
        {
            BalanceProduction(MacroData.Units);

            var nexuss = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var gateways = UnitManager.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var robos = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ROBOTICSFACILITY && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
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
        }

        public void BalanceTerranProduction()
        {
            BalanceProduction(MacroData.Units);

            var commandCenters = UnitManager.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var barracks = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var factories = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var starports = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();

            var unitTypes = new List<UnitTypes>();
            for (int index = 0; index < commandCenters; index++)
            {
                unitTypes.AddRange(MacroData.CommandCenterUnits);
            }
            for (int index = 0; index < barracks; index++)
            {
                unitTypes.AddRange(MacroData.BarracksUnits);
            }
            for (int index = 0; index < factories; index++)
            {
                unitTypes.AddRange(MacroData.FactoryUnits);
            }
            for (int index = 0; index < starports; index++)
            {
                unitTypes.AddRange(MacroData.StarportUnits);
            }

            BalanceProduction(unitTypes);

            if (MacroData.CommandCenterUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.CommandCenterUnits);
            }
            if (MacroData.BarracksUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.BarracksUnits);
            }
            if (MacroData.FactoryUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.FactoryUnits);
            }
            if (MacroData.StarportUnits.Where(u => MacroData.BuildUnits[u]).Count() > 1)
            {
                BalanceProduction(MacroData.StarportUnits);
            }
        }

        public void BalanceProduction(List<UnitTypes> unitTypes)
        {
            var desiredTotal = unitTypes.Sum(u => MacroData.DesiredUnitCounts[u]);
            var currentTotal = unitTypes.Sum(u => UnitManager.Count(u) + UnitManager.UnitsInProgressCount(u));
            var numberOfTypes = unitTypes.Count();

            foreach (var u in unitTypes)
            {
                var desiredRatio = MacroData.DesiredUnitCounts[u] / (double)desiredTotal;

                var count = UnitManager.Count(u) + UnitManager.UnitsInProgressCount(u);
                if (u == UnitTypes.PROTOSS_ARCHON)
                {
                    continue;
                }

                var trainingData = UnitDataManager.TrainingData[u];
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
