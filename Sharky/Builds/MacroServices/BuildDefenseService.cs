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
        MapDataService MapDataService;
        WallService WallService;

        int defensivePointLastFailFrame;

        public BuildDefenseService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, BaseData baseData, TargetingData targetingData, BuildOptions buildOptions, BuildingService buildingService, MapDataService mapDataService, WallService wallService)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            TargetingData = targetingData;
            BuildOptions = buildOptions;
            BuildingService = buildingService;
            MapDataService = mapDataService;
            WallService = wallService;

            defensivePointLastFailFrame = 0;
            WallService = wallService;
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
                var height = MapDataService.MapHeight(TargetingData.ForwardDefensePoint);
                foreach (var unit in MacroData.DesiredDefensiveBuildingsAtDefensivePoint)
                {
                    if (unit.Value > 0)
                    {
                        var maxDistance = MacroData.DefensiveBuildingMaximumDistance;
                        if (BaseData.BaseLocations.Any(b => b.Location.X == TargetingData.ForwardDefensePoint.X && b.Location.Y == TargetingData.ForwardDefensePoint.Y))
                        {
                            maxDistance = 25;
                        }

                        var unitData = SharkyUnitData.BuildingData[unit.Key];
                        var matchedBuildings = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)) < maxDistance * maxDistance && MapDataService.MapHeight(u.Value.Position) == height);
                        int builtCount = GetBuildingCount(matchedBuildings, unit.Key);

                        if (builtCount + ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability)) < unit.Value)
                        {
                            var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, TargetingData.ForwardDefensePoint, false, maxDistance, wallOffType: BuildOptions.WallOffType, requireSameHeight: true);
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

        private int GetBuildingCount(IEnumerable<KeyValuePair<ulong, UnitCalculation>> matchedBuildings, UnitTypes unitType)
        {
            var count = matchedBuildings.Count();
            if (BuildOptions.WallOffType == WallOffType.None || MapDataService?.MapData?.WallData == null) { return count; }

            var baseLocation = WallService.GetBaseLocation();
            if (baseLocation == null) { return count; }

            WallData wallData = null;
            if (BuildOptions.WallOffType == WallOffType.Partial)
            {
                wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == baseLocation.X && b.BasePosition.Y == baseLocation.Y);
            }
            else if (BuildOptions.WallOffType == WallOffType.Terran)
            {
                wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == baseLocation.X && b.BasePosition.Y == baseLocation.Y);
                if (wallData == null)
                {
                    var firstBase = BaseData.SelfBases.FirstOrDefault();
                    if (firstBase != null)
                    {
                        wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == firstBase.Location.X && b.BasePosition.Y == firstBase.Location.Y);
                    }
                }
            }

            if (wallData == null) { return count; }

            var otherBuildings = ActiveUnitData.SelfUnits.Values.Where(c => c.Unit.UnitType == (uint)unitType && !matchedBuildings.Any(m => m.Value.Unit.Tag == c.Unit.Tag));
            if (unitType == UnitTypes.TERRAN_BUNKER && wallData.Bunkers != null)
            {
                foreach (var spot in wallData.Bunkers)
                {
                    if (otherBuildings.Any(b => b.Unit.Pos.X == spot.X && b.Unit.Pos.Y == spot.Y))
                    {
                        count++;
                    }
                }
            }
            else if (wallData.WallSegments != null)
            {
                foreach (var spot in wallData.WallSegments)
                {
                    if (otherBuildings.Any(b => b.Unit.Pos.X == spot.Position.X && b.Unit.Pos.Y == spot.Position.Y))
                    {
                        count++;
                    }
                }
            }
            

            return count;
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
                        var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                        foreach (var baseLocation in BaseData.SelfBases)
                        {
                            var height = MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation);
                            if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                            {
                                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < unit.Value)
                                {
                                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance, requireSameHeight: true, wallOffType: WallOffType.None);
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

                    var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                    var baseLocation = BuildingService.GetNextBaseLocation();

                    if (baseLocation != null && baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                    {
                        var height = MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation);
                        if (MacroData.Minerals >= unitData.Minerals && MacroData.VespeneGas >= unitData.Gas)
                        {
                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance, requireSameHeight: true, wallOffType: BuildOptions.WallOffType, allowBlockBase: false);
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

                    var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                    foreach (var baseLocation in BaseData.SelfBases)
                    {
                        if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                        {
                            var height = MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation);
                            if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unit.Key && Vector2.DistanceSquared(u.Value.Position, baseLocation.MineralLineBuildingLocation.ToVector2()) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < unit.Value)
                            {
                                var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, baseLocation.MineralLineBuildingLocation, true, MacroData.DefensiveBuildingMineralLineMaximumDistance, requireSameHeight: true, wallOffType: WallOffType.None);
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
