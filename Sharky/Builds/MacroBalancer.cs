using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds
{
    public class MacroBalancer : IMacroBalancer
    {
        BuildOptions BuildOptions;
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        UnitCountService UnitCountService;

        public MacroBalancer(BuildOptions buildOptions, ActiveUnitData activeUnitData, MacroData macroData, SharkyUnitData sharkyUnitData, BaseData baseData, UnitCountService unitCountService)
        {
            BuildOptions = buildOptions;
            ActiveUnitData = activeUnitData;
            MacroData = macroData;
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            UnitCountService = unitCountService;
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
            else
            {
                BalanceOverlords();
            }
        }

        void BalancePylons()
        {
            if (!BuildOptions.StrictSupplyCount)
            {
                var productionCapacity = UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) + (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) * 2) + (UnitCountService.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) * 6) + (UnitCountService.Count(UnitTypes.PROTOSS_STARGATE) * 6);
                MacroData.DesiredPylons = (int)Math.Ceiling(((MacroData.FoodUsed - (UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) * 15)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroData.BuildPylon = MacroData.DesiredPylons > UnitCountService.Count(UnitTypes.PROTOSS_PYLON) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON));
        }

        void BalanceSupplyDepots()
        {
            if (!BuildOptions.StrictSupplyCount)
            {
                var productionCapacity = UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) + (UnitCountService.Count(UnitTypes.TERRAN_BARRACKS) * 2) + (UnitCountService.Count(UnitTypes.TERRAN_FACTORY) * 6) + (UnitCountService.Count(UnitTypes.TERRAN_STARPORT) * 6);
                MacroData.DesiredSupplyDepots = (int)Math.Ceiling(((MacroData.FoodUsed - (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) * 12)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroData.BuildSupplyDepot = UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_SUPPLYDEPOT)) < MacroData.DesiredSupplyDepots;
        }

        void BalanceOverlords()
        {
            if (!BuildOptions.StrictSupplyCount)
            {
                var productionCapacity = UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY) * 8;
                MacroData.DesiredOverlords = (int)Math.Ceiling(((MacroData.FoodUsed - (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY) * 6)) / 8.0) + (productionCapacity / 8.0));
            }

            MacroData.BuildOverlord = MacroData.DesiredOverlords > UnitCountService.Count(UnitTypes.ZERG_OVERLORD) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_EGG && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.TRAIN_OVERLORD));
        }

        public void BalanceGases()
        {
            if (!BuildOptions.StrictGasCount)
            {
                MacroData.DesiredGases = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter) && c.UnitCalculation.Unit.BuildProgress > .7) * 2;
                if (MacroData.DesiredGases > 2 && MacroData.VespeneGas > 300 && MacroData.Minerals < 150) // if we have an abundance of gas don't build more
                {
                    MacroData.DesiredGases = 2;
                }
            }

            var assimilator = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_ASSIMILATOR];
            var extractor = SharkyUnitData.BuildingData[UnitTypes.ZERG_EXTRACTOR];
            var refinery = SharkyUnitData.BuildingData[UnitTypes.TERRAN_REFINERY];
            var workerOrders = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)assimilator.Ability || o.AbilityId == (uint)extractor.Ability || o.AbilityId == (uint)refinery.Ability));
            var gases = BaseData.SelfBases.Sum(b => b.GasMiningInfo.Count());
            if (MacroData.Race == Race.Terran)
            {
                if (workerOrders > 0 && gases > 0)
                {
                    gases = BaseData.SelfBases.Sum(b => b.GasMiningInfo.Count(g => g.ResourceUnit.BuildProgress == 1));
                }
            }
            MacroData.BuildGas = MacroData.DesiredGases > gases + workerOrders;
        }

        public void BalanceTech()
        {
            foreach (var u in MacroData.Tech)
            {
                var unitData = SharkyUnitData.BuildingData[u];
                MacroData.BuildTech[u] = UnitCountService.EquivalentTypeCount(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredTechCounts[u];
            }
        }

        public void BalanceAddOns()
        {
            foreach (var u in MacroData.AddOns)
            {
                MacroData.BuildAddOns[u] = UnitCountService.Count(u) < MacroData.DesiredAddOnCounts[u];
            }
        }

        public void BalanceDefensiveBuildings()
        {
            foreach (var u in MacroData.DefensiveBuildings)
            {
                var unitData = SharkyUnitData.BuildingData[u];
                if (MacroData.Race == Race.Protoss)
                {
                    MacroData.BuildDefensiveBuildings[u] = UnitCountService.EquivalentTypeCount(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredDefensiveBuildingsCounts[u];
                }
                else
                {
                    MacroData.BuildDefensiveBuildings[u] = UnitCountService.EquivalentTypeCompleted(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredDefensiveBuildingsCounts[u];
                }
            }
        }

        public void BalanceProductionBuildings()
        {
            foreach (var u in MacroData.Production)
            {
                var unitData = SharkyUnitData.BuildingData[u];
                if (MacroData.Race == Race.Protoss)
                {
                    MacroData.BuildProduction[u] = UnitCountService.EquivalentTypeCount(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredProductionCounts[u];
                }
                else if (MacroData.Race == Race.Zerg)
                {
                    MacroData.BuildProduction[u] = UnitCountService.EquivalentTypeCount(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredProductionCounts[u];
                }
                else
                {
                    MacroData.BuildProduction[u] = UnitCountService.EquivalentTypeCompleted(u) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredProductionCounts[u];
                }
            }
        }

        public void BalanceMorphs()
        {
            foreach (var u in MacroData.Morphs)
            {
                var unitData = SharkyUnitData.MorphData[u];
                MacroData.Morph[u] = UnitCountService.EquivalentTypeCount(u) + ActiveUnitData.Commanders.Values.Count(c => unitData.ProducingUnits.Contains((UnitTypes)c.UnitCalculation.Unit.UnitType) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < MacroData.DesiredMorphCounts[u];
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
            else
            {
                BalanceZergProduction();
            }

            var workerType = UnitTypes.PROTOSS_PROBE;
            if (MacroData.Race == Race.Terran)
            {
                workerType = UnitTypes.TERRAN_SCV;
            }
            else if (MacroData.Race == Race.Zerg)
            {
                workerType = UnitTypes.ZERG_DRONE;
            }
            if (!BuildOptions.StrictWorkerCount)
            {
                var resourceCenters = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                var completedResourceCenters = resourceCenters.Where(n => n.UnitCalculation.Unit.BuildProgress == 1);
                var buildingResourceCentersCount = resourceCenters.Count(n => n.UnitCalculation.Unit.BuildProgress < 1);
                var desiredWorkers = completedResourceCenters.Sum(n => n.UnitCalculation.Unit.IdealHarvesters + 6) + (buildingResourceCentersCount * 22) + 1; // +6 because gas, +1 to build
                if (desiredWorkers < 40)
                {
                    desiredWorkers = 40;
                }
                if (desiredWorkers > 90)
                {
                    desiredWorkers = 90;
                }

                var doneAndInProgress = UnitCountService.UnitsDoneAndInProgressCount(workerType);

                MacroData.DesiredUnitCounts[workerType] = desiredWorkers;

                if (UnitCountService.Count(workerType) < desiredWorkers && UnitCountService.Count(workerType) + UnitCountService.UnitsInProgressCount(workerType) < desiredWorkers)
                {
                    MacroData.BuildUnits[workerType] = true;
                }
                else
                {
                    MacroData.BuildUnits[workerType] = false;
                }
            }

            if (BuildOptions.OnlyBuildWorkersWithExtraMinerals && MacroData.BuildUnits[workerType])
            {
                MacroData.BuildUnits[workerType] = false;
                if (!MacroData.BuildAddOns.Any(e => e.Value) && !MacroData.BuildDefensiveBuildings.Any(e => e.Value) && !MacroData.BuildGas && !MacroData.BuildOverlord && !MacroData.BuildProduction.Any(e => e.Value) && !MacroData.BuildPylon && !MacroData.BuildSupplyDepot && !MacroData.BuildTech.Any(e => e.Value))
                {
                    if (!MacroData.BuildUnits.Any(e => e.Value) || MacroData.Minerals > 450)
                    {
                        MacroData.BuildUnits[workerType] = true;
                    }
                }
            }
        }

        public void BalanceProtossProduction()
        {
            BalanceProduction(MacroData.Units);

            var nexuss = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var gateways = ActiveUnitData.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var robos = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ROBOTICSFACILITY && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var stargates = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STARGATE && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();

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

            var commandCenters = ActiveUnitData.Commanders.Values.Where(c => (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS) && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var barracks = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var factories = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();
            var starports = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT && c.UnitCalculation.Unit.BuildProgress == 1 && !c.UnitCalculation.Unit.IsActive).Count();

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

        public void BalanceZergProduction()
        {
            BalanceProduction(MacroData.Units);
        }

        public void BalanceProduction(List<UnitTypes> unitTypes)
        {
            var desiredTotal = unitTypes.Sum(u => MacroData.DesiredUnitCounts[u]);
            var currentTotal = unitTypes.Sum(u => UnitCountService.EquivalentTypeCount(u) + UnitCountService.UnitsInProgressCount(u));
            var numberOfTypes = unitTypes.Count();

            foreach (var u in unitTypes)
            {
                var desiredRatio = (MacroData.DesiredUnitCounts[u] + 1) / (double)desiredTotal;

                var count = UnitCountService.EquivalentTypeCount(u) + UnitCountService.UnitsInProgressCount(u);
                if (u == UnitTypes.PROTOSS_ARCHON || u == UnitTypes.ZERG_QUEEN)
                {
                    MacroData.BuildUnits[u] = count < MacroData.DesiredUnitCounts[u];
                    continue;
                }

                var trainingData = SharkyUnitData.TrainingData[u];

                var actualRatio = count / (double)currentTotal;

                if (MacroData.DesiredUnitCounts[u] == 1 && count == 0)
                {
                    MacroData.BuildUnits[u] = true;
                }
                else if (actualRatio > desiredRatio || count >= MacroData.DesiredUnitCounts[u])
                {
                    MacroData.BuildUnits[u] = false;
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
