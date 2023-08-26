namespace Sharky.Builds.MacroServices
{
    public class BuildDefenseService
    {
        MacroData MacroData;
        IBuildingBuilder BuildingBuilder;
        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        TargetingData TargetingData;
        BuildOptions BuildOptions;
        BuildingService BuildingService;

        int defensivePointLastFailFrame;

        public BuildDefenseService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, BaseData baseData, TargetingData targetingData, BuildOptions buildOptions, BuildingService buildingService)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            TargetingData = targetingData;
            BuildOptions = buildOptions;
            BuildingService = buildingService;

            defensivePointLastFailFrame = 0;
        }

        public List<SC2Action> BuildDefensiveBuildings()
        {
            var commands = new List<SC2Action>();

            foreach (var unit in MacroData.BuildDefensiveBuildings)
            {
                if (unit.Value)
                {
                    var unitData = SharkyUnitData.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, TargetingData.ForwardDefensePoint, wallOffType: BuildOptions.WallOffType);
                    if (command != null)
                    {
                        commands.AddRange(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        public List<SC2Action> BuildDefensiveBuildingsAtDefensivePoint()
        {
            var commands = new List<SC2Action>();

            if (defensivePointLastFailFrame < MacroData.Frame - 100)
            {
                foreach (var unit in MacroData.DesiredDefensiveBuildingsAtDefensivePoint)
                {
                    if (unit.Value > 0)
                    {
                        var unitData = SharkyUnitData.BuildingData[unit.Key];
                        if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < unit.Value)
                        {
                            var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, TargetingData.ForwardDefensePoint, false, MacroData.DefensiveBuildingMaximumDistance, wallOffType: BuildOptions.WallOffType);
                            if (command != null)
                            {
                                commands.AddRange(command);
                                return commands;
                            }
                            else
                            {
                                defensivePointLastFailFrame = MacroData.Frame;
                            }
                        }
                    }
                }
            }

            return commands;
        }

        public List<SC2Action> BuildDefensiveBuildingsAtEveryBase()
        {
            var commands = new List<SC2Action>();

            foreach (var unit in MacroData.DesiredDefensiveBuildingsAtEveryBase)
            {
                if (unit.Value > 0)
                {
                    var unitData = SharkyUnitData.BuildingData[unit.Key];

                    if (unitData.Minerals <= MacroData.Minerals && unitData.Gas <= MacroData.VespeneGas)
                    {
                        var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                        foreach (var baseLocation in BaseData.SelfBases)
                        {
                            if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                            {
                                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < unit.Value)
                                {
                                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance, wallOffType: BuildOptions.WallOffType);
                                    if (command != null)
                                    {
                                        commands.AddRange(command);
                                        return commands;
                                    }
                                    else
                                    {
                                        baseLocation.MineralLineDefenseUnbuildableFrame = MacroData.Frame;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return commands;
        }

        public List<SC2Action> BuildDefensiveBuildingsAtNextBase()
        {
            var commands = new List<SC2Action>();

            foreach (var unit in MacroData.DesiredDefensiveBuildingsAtNextBase)
            {
                if (unit.Value > 0)
                {
                    var unitData = SharkyUnitData.BuildingData[unit.Key];

                    var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                    var baseLocation = BuildingService.GetNextBaseLocation();

                    if (baseLocation != null && baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                    {
                        if (MacroData.Minerals >= unitData.Minerals && MacroData.VespeneGas >= unitData.Gas)
                        {
                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance, wallOffType: BuildOptions.WallOffType, allowBlockBase: false);
                                if (command != null)
                                {
                                    commands.AddRange(command);
                                    return commands;
                                }
                                else
                                {
                                    baseLocation.MineralLineDefenseUnbuildableFrame = MacroData.Frame;
                                }
                            }
                        }
                    }

                }
            }

            return commands;
        }

        public IEnumerable<SC2Action> BuildDefensiveBuildingsAtEveryMineralLine()
        {
            var commands = new List<SC2Action>();

            foreach (var unit in MacroData.DesiredDefensiveBuildingsAtEveryMineralLine)
            {
                if (unit.Value > 0)
                {
                    var unitData = SharkyUnitData.BuildingData[unit.Key];

                    var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                    foreach (var baseLocation in BaseData.SelfBases)
                    {
                        if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                        {
                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.MineralLineBuildingLocation.X, baseLocation.MineralLineBuildingLocation.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.MineralLineBuildingLocation, true, MacroData.DefensiveBuildingMineralLineMaximumDistance, wallOffType: BuildOptions.WallOffType);
                                if (command != null)
                                {
                                    commands.AddRange(command);
                                    return commands;
                                }
                                else
                                {
                                    baseLocation.MineralLineDefenseUnbuildableFrame = MacroData.Frame;
                                }
                            }
                        }
                    }

                }
            }

            return commands;
        }
    }
}
