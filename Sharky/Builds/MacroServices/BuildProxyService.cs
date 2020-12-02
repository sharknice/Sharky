using SC2APIProtocol;
using Sharky.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.MacroServices
{
    public class BuildProxyService
    {
        MacroData MacroData;
        IBuildingBuilder BuildingBuilder;
        UnitDataManager UnitDataManager;
        IUnitManager UnitManager;
        IBaseManager BaseManager;
        ITargetingManager TargetingManager;
        Morpher Morpher;
        MicroManager MicroManager;

        int lastFailFrame;

        public BuildProxyService(MacroData macroData, IBuildingBuilder buildingBuilder, UnitDataManager unitDataManager, IUnitManager unitManager, IBaseManager baseManager, ITargetingManager targetingManager, Morpher morpher, MicroManager microManager)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            UnitDataManager = unitDataManager;
            UnitManager = unitManager;
            BaseManager = baseManager;
            TargetingManager = targetingManager;
            Morpher = morpher;
            MicroManager = microManager;

            lastFailFrame = 0;
        }

        public IEnumerable<Action> BuildPylons()
        {
            var commands = new List<Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];
                var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                foreach (var proxy in MacroData.Proxies)
                {
                    if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < proxy.Value.DesiredPylons)
                    {
                        var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroManager.MicroTasks[proxy.Key].UnitCommanders);
                        if (command != null)
                        {
                            commands.Add(command);
                            return commands;
                        }
                        else
                        {
                            lastFailFrame = MacroData.Frame;
                        }
                    }
                }
            }

            return commands;
        }

        public IEnumerable<Action> BuildDefensiveBuildings()
        {
            var commands = new List<Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies)
                {
                    foreach (var unit in proxy.Value.DesiredDefensiveBuildingsCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = UnitDataManager.BuildingData[unit.Key];

                            var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                            if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroManager.MicroTasks[proxy.Key].UnitCommanders);
                                if (command != null)
                                {
                                    commands.Add(command);
                                    return commands;
                                }
                                else
                                {
                                    lastFailFrame = MacroData.Frame;
                                }
                            }
                        }
                    }
                }
            }

            return commands;
        }

        public IEnumerable<Action> BuildProductionBuildings()
        {
            var commands = new List<Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies)
                {
                    foreach (var unit in proxy.Value.DesiredProductionCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = UnitDataManager.BuildingData[unit.Key];

                            var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                            if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroManager.MicroTasks[proxy.Key].UnitCommanders);
                                if (command != null)
                                {
                                    commands.Add(command);
                                    return commands;
                                }
                                else
                                {
                                    lastFailFrame = MacroData.Frame;
                                }
                            }
                        }
                    }
                }
            }

            return commands;
        }

        public IEnumerable<Action> MorphBuildings()
        {
            var commands = new List<Action>();
            // TODO: check morph location needs to be the proxied one
            foreach (var unit in MacroData.Morph)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.MorphData[unit.Key];
                    var command = Morpher.MorphBuilding(MacroData, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        public IEnumerable<Action> BuildTechBuildings()
        {
            var commands = new List<Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies)
                {
                    foreach (var unit in proxy.Value.DesiredTechCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = UnitDataManager.BuildingData[unit.Key];

                            var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                            if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroManager.MicroTasks[proxy.Key].UnitCommanders);
                                if (command != null)
                                {
                                    commands.Add(command);
                                    return commands;
                                }
                                else
                                {
                                    lastFailFrame = MacroData.Frame;
                                }
                            }
                        }
                    }
                }
            }

            return commands;
        }

        public IEnumerable<Action> BuildAddOns()
        {
            var commands = new List<Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies)
                {
                    foreach (var unit in proxy.Value.DesiredAddOnCounts)
                    {
                        if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) < unit.Value)
                        {
                            var unitData = UnitDataManager.AddOnData[unit.Key];
                            var command = BuildingBuilder.BuildAddOn(MacroData, unitData, proxy.Value.Location);
                            if (command != null)
                            {
                                commands.Add(command);
                                continue;
                            }
                        }
                    }
                }

            }



            return commands;
        }
    }
}
