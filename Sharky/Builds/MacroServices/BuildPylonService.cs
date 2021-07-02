using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        int defensivePointLastFailFrame;

        public BuildPylonService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, BaseData baseData, TargetingData targetingData, BuildingService buildingService)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            TargetingData = targetingData;
            BuildingService = buildingService;

            defensivePointLastFailFrame = 0;
        }

        public List<SC2APIProtocol.Action> BuildPylon(Point2D location, bool ignoreMineralProximity = false, float maxDistance = 50)
        {
            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
            return BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, location, ignoreMineralProximity, maxDistance);
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
                    if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryBase)
                    {
                        var command = BuildPylon(baseLocation.Location, true, MacroData.DefensiveBuildingMaximumDistance);
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
            var baseLocation = GetNextBaseLocation();

            if (baseLocation != null && baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
            {
                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtNextBase)
                {
                    var command = BuildPylon(baseLocation.Location, false, MacroData.DefensiveBuildingMaximumDistance);
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

        public IEnumerable<Action> BuildPylonsAtEveryMineralLine()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseData.SelfBases)
            {
                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(baseLocation.MineralLineBuildingLocation.X, baseLocation.MineralLineBuildingLocation.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryMineralLine)
                {
                    var command = BuildPylon(baseLocation.MineralLineBuildingLocation, true, MacroData.DefensiveBuildingMineralLineMaximumDistance);
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

        public IEnumerable<Action> BuildPylonsAtDefensivePoint()
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (defensivePointLastFailFrame < MacroData.Frame - 100)
            {
                var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

                var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(u.Value.Position, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtDefensivePoint)
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

        private BaseLocation GetNextBaseLocation()
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (BuildingService.AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !BuildingService.Blocked(openBase.Location.X, openBase.Location.Y, 2))
                {
                    return openBase;
                }

            }
            return null;
        }
    }
}
