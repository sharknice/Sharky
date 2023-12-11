namespace Sharky.Builds.MacroServices
{
    public class BuildPylonService
    {
        MacroData MacroData;
        IBuildingBuilder BuildingBuilder;
        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        TargetingData TargetingData;
        BuildingService BuildingService;
        MapDataService MapDataService;

        int defensivePointLastFailFrame;

        public BuildPylonService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, BaseData baseData, TargetingData targetingData, BuildingService buildingService, MapDataService mapDataService)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            TargetingData = targetingData;
            BuildingService = buildingService;
            MapDataService = mapDataService;

            defensivePointLastFailFrame = 0;
        }

        public List<SC2APIProtocol.Action> BuildPylon(Point2D location, bool ignoreMineralProximity = false, float maxDistance = 50, bool allowBlockBase = false, bool requireSameHeight = false)
        {
            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
            return BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, location, ignoreMineralProximity, maxDistance, allowBlockBase: allowBlockBase, requireSameHeight: requireSameHeight);
        }

        public IEnumerable<SC2APIProtocol.Action> BuildPylonsAtEveryBase()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseData.SelfBases)
            {
                if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                {
                    var height = MapDataService.MapHeight(baseLocation.Location);
                    if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < MacroData.ProtossMacroData.DesiredPylonsAtEveryBase)
                    {
                        var command = BuildPylon(baseLocation.Location, true, MacroData.DefensiveBuildingMaximumDistance, requireSameHeight: true);
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

            return commands;
        }

        public IEnumerable<SC2APIProtocol.Action> BuildPylonsAtNextBase()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            var baseLocation = BuildingService.GetNextBaseLocation();

            if (baseLocation != null && baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
            {
                var height = MapDataService.MapHeight(baseLocation.Location);

                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < MacroData.ProtossMacroData.DesiredPylonsAtNextBase)
                {
                    var command = BuildPylon(baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance, false, true);
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

            return commands;
        }

        public IEnumerable<SC2Action> BuildPylonsAtEveryMineralLine()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseData.SelfBases)
            {
                var height = MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation);
                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, baseLocation.MineralLineBuildingLocation.ToVector2()) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < MacroData.ProtossMacroData.DesiredPylonsAtEveryMineralLine)
                {
                    var command = BuildPylon(baseLocation.MineralLineBuildingLocation, true, MacroData.DefensiveBuildingMineralLineMaximumDistance, requireSameHeight: true);
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

            return commands;
        }

        public IEnumerable<SC2Action> BuildPylonsAtDefensivePoint()
        {
            var commands = new List<SC2Action>();

            if (defensivePointLastFailFrame < MacroData.Frame - 100)
            {
                var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

                var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
                var height = MapDataService.MapHeight(TargetingData.ForwardDefensePoint);
                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance && MapDataService.MapHeight(u.Value.Position) == height) + orderedBuildings < MacroData.ProtossMacroData.DesiredPylonsAtDefensivePoint)
                {
                    var command = BuildPylon(TargetingData.ForwardDefensePoint, true, MacroData.DefensiveBuildingMineralLineMaximumDistance);
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

            return commands;
        }
    }
}
