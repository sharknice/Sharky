namespace Sharky.Builds.MacroServices
{
    public class BuildProxyService
    {
        MacroData MacroData;
        IBuildingBuilder BuildingBuilder;
        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;
        Morpher Morpher;
        MicroTaskData MicroTaskData;

        int lastFailFrame;

        public BuildProxyService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, Morpher morpher, MicroTaskData microTaskData)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            Morpher = morpher;
            MicroTaskData = microTaskData;

            lastFailFrame = 0;
        }

        public IEnumerable<SC2Action> BuildPylons()
        {
            var commands = new List<SC2Action>();

            if (MacroData.Minerals >= 100 && lastFailFrame < MacroData.Frame - 100)
            {
                var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
                var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                foreach (var proxy in MacroData.Proxies.Where(p => p.Value.Enabled))
                {
                    if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < proxy.Value.DesiredPylons)
                    {
                        var location = proxy.Value.Location;
                        if (proxy.Value.HardCodedBuildingData?.Pylons != null)
                        {
                            foreach (var spot in proxy.Value.HardCodedBuildingData.Pylons)
                            {
                                if (!ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.Position.X == spot.X && u.Value.Position.Y == spot.Y))
                                {
                                    location = spot;
                                    break;
                                }
                            }                      
                        }

                        var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, location, true, proxy.Value.MaximumBuildingDistance, MicroTaskData[proxy.Key].UnitCommanders, allowBlockBase: true);
                        if (command != null)
                        {
                            commands.AddRange(command);
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

        public IEnumerable<SC2Action> BuildDefensiveBuildings()
        {
            var commands = new List<SC2Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies.Where(p => p.Value.Enabled))
                {
                    foreach (var unit in proxy.Value.DesiredDefensiveBuildingsCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = SharkyUnitData.BuildingData[unit.Key];

                            if (MacroData.Minerals >= unitData.Minerals && MacroData.VespeneGas >= unitData.Gas)
                            {
                                var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                                {
                                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroTaskData[proxy.Key].UnitCommanders, allowBlockBase: true);
                                    if (command != null)
                                    {
                                        commands.AddRange(command);
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
            }

            return commands;
        }

        public IEnumerable<SC2Action> BuildProductionBuildings()
        {
            var commands = new List<SC2Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies.Where(p => p.Value.Enabled))
                {
                    foreach (var unit in proxy.Value.DesiredProductionCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = SharkyUnitData.BuildingData[unit.Key];

                            if (unitData.Gas > MacroData.VespeneGas || unitData.Minerals > MacroData.Minerals)
                            {
                                continue;
                            }

                            var orderedBuildings = 0;
                            if (MacroData.Race != Race.Terran)
                            {
                                ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                            }              

                            if (unit.Key == UnitTypes.PROTOSS_GATEWAY)
                            {
                                orderedBuildings += ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance);
                            }

                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                            {
                                if (MicroTaskData[proxy.Key].UnitCommanders.Any(c => c.UnitCalculation.Unit.Orders.Count() == 0 || c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId != (uint)unitData.Ability)))
                                {
                                    var location = proxy.Value.Location;
                                    if (proxy.Value.HardCodedBuildingData?.Production != null)
                                    {
                                        foreach (var spot in proxy.Value.HardCodedBuildingData.Production)
                                        {
                                            if (!ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)unit.Key && u.Value.Position.X == spot.X && u.Value.Position.Y == spot.Y))
                                            {
                                                location = spot;
                                                break;
                                            }
                                        }
                                    }

                                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, location, true, proxy.Value.MaximumBuildingDistance, MicroTaskData[proxy.Key].UnitCommanders, allowBlockBase: true);
                                    if (command != null)
                                    {
                                        commands.AddRange(command);
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
            }

            return commands;
        }

        public IEnumerable<SC2Action> MorphBuildings()
        {
            var commands = new List<SC2Action>();
            // TODO: check morph location needs to be the proxied one
            foreach (var unit in MacroData.Morph)
            {
                if (unit.Value)
                {
                    var unitData = SharkyUnitData.MorphData[unit.Key];
                    var command = Morpher.MorphBuilding(MacroData, unitData);
                    if (command != null)
                    {
                        commands.AddRange(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        public IEnumerable<SC2Action> BuildTechBuildings()
        {
            var commands = new List<SC2Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies.Where(p => p.Value.Enabled))
                {
                    foreach (var unit in proxy.Value.DesiredTechCounts)
                    {
                        if (unit.Value > 0)
                        {
                            var unitData = SharkyUnitData.BuildingData[unit.Key];

                            if (unitData.Gas > MacroData.VespeneGas || unitData.Minerals > MacroData.Minerals)
                            {
                                continue;
                            }

                            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, proxy.Value.Location, true, proxy.Value.MaximumBuildingDistance, MicroTaskData[proxy.Key].UnitCommanders, allowBlockBase: true);
                                if (command != null)
                                {
                                    commands.AddRange(command);
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

        public IEnumerable<SC2Action> BuildAddOns()
        {
            var commands = new List<SC2Action>();

            if (lastFailFrame < MacroData.Frame - 100)
            {
                foreach (var proxy in MacroData.Proxies.Where(p => p.Value.Enabled))
                {
                    foreach (var unit in proxy.Value.DesiredAddOnCounts)
                    {
                        if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(proxy.Value.Location.X, proxy.Value.Location.Y)) < proxy.Value.MaximumBuildingDistance * proxy.Value.MaximumBuildingDistance) < unit.Value)
                        {
                            var unitData = SharkyUnitData.AddOnData[unit.Key];
                            var command = BuildingBuilder.BuildAddOn(MacroData, unitData, proxy.Value.Location);
                            if (command != null)
                            {
                                commands.AddRange(command);
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
