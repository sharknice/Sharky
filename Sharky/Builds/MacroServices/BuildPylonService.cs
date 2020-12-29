using SC2APIProtocol;
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

        int defensivePointLastFailFrame;

        public BuildPylonService(MacroData macroData, IBuildingBuilder buildingBuilder, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, BaseData baseData, TargetingData targetingData)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            TargetingData = targetingData;

            defensivePointLastFailFrame = 0;
        }

        public SC2APIProtocol.Action BuildPylon(Point2D location, bool ignoreMineralProximity = false, float maxDistance = 50)
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
                    if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryBase)
                    {
                        var command = BuildPylon(baseLocation.Location, true, MacroData.DefensiveBuildingMaximumDistance);
                        if (command != null)
                        {
                            commands.Add(command);
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

        public IEnumerable<Action> BuildPylonsAtEveryMineralLine()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseData.SelfBases)
            {
                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(baseLocation.MineralLineLocation.X, baseLocation.MineralLineLocation.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryMineralLine)
                {
                    var command = BuildPylon(baseLocation.MineralLineLocation, true, MacroData.DefensiveBuildingMineralLineMaximumDistance);
                    if (command != null)
                    {
                        commands.Add(command);
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

                if (ActiveUnitData.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(TargetingData.MainDefensePoint.X, TargetingData.MainDefensePoint.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtDefensivePoint)
                {
                    var command = BuildPylon(TargetingData.MainDefensePoint, true, MacroData.DefensiveBuildingMineralLineMaximumDistance);
                    if (command != null)
                    {
                        commands.Add(command);
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
