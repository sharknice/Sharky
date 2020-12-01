using SC2APIProtocol;
using Sharky.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.MacroServices
{
    public class BuildPylonService
    {
        MacroData MacroData;
        IBuildingBuilder BuildingBuilder;
        UnitDataManager UnitDataManager;
        UnitManager UnitManager;
        BaseManager BaseManager;
        TargetingManager TargetingManager;

        int defensivePointLastFailFrame;

        public BuildPylonService(MacroData macroData, IBuildingBuilder buildingBuilder, UnitDataManager unitDataManager, UnitManager unitManager, BaseManager baseManager, TargetingManager targetingManager)
        {
            MacroData = macroData;
            BuildingBuilder = buildingBuilder;
            UnitDataManager = unitDataManager;
            UnitManager = unitManager;
            BaseManager = baseManager;
            TargetingManager = targetingManager;

            defensivePointLastFailFrame = 0;
        }

        public SC2APIProtocol.Action BuildPylon(Point2D location, bool ignoreMineralProximity = false, float maxDistance = 50)
        {
            var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];
            return BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, location, ignoreMineralProximity, maxDistance);
        }

        public IEnumerable<SC2APIProtocol.Action> BuildPylonsAtEveryBase()
        {
            var commands = new List<SC2APIProtocol.Action>();

            var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseManager.SelfBases)
            {
                if (baseLocation.MineralLineDefenseUnbuildableFrame < MacroData.Frame - 100)
                {
                    if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(baseLocation.Location.X, baseLocation.Location.Y)) < MacroData.DefensiveBuildingMaximumDistance * MacroData.DefensiveBuildingMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryBase)
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

            var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];

            var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
            foreach (var baseLocation in BaseManager.SelfBases)
            {
                if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(baseLocation.MineralLineLocation.X, baseLocation.MineralLineLocation.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtEveryMineralLine)
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
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];

                var orderedBuildings = UnitManager.Commanders.Values.Count(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));

                if (UnitManager.SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(TargetingManager.DefensePoint.X, TargetingManager.DefensePoint.Y)) < MacroData.DefensiveBuildingMineralLineMaximumDistance * MacroData.DefensiveBuildingMineralLineMaximumDistance) + orderedBuildings < MacroData.DesiredPylonsAtDefensivePoint)
                {
                    var command = BuildPylon(TargetingManager.DefensePoint, true, MacroData.DefensiveBuildingMineralLineMaximumDistance);
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
