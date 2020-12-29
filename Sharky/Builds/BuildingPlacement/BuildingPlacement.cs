using SC2APIProtocol;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingPlacement : IBuildingPlacement
    {
        IBuildingPlacement ProtossBuildingPlacement;
        IBuildingPlacement TerranBuildingPlacement;
        IBuildingPlacement ZergBuildingPlacement;
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        BuildingService BuildingService;
        SharkyUnitData SharkyUnitData;

        public BuildingPlacement(IBuildingPlacement protossBuildingPlacement, IBuildingPlacement terranBuildingPlacement, IBuildingPlacement zergBuildingPlacement, BaseData baseData, ActiveUnitData activeUnitData, BuildingService buildingService, SharkyUnitData sharkyUnitData)
        {
            ProtossBuildingPlacement = protossBuildingPlacement;
            TerranBuildingPlacement = terranBuildingPlacement;
            ZergBuildingPlacement = zergBuildingPlacement;
            BaseData = baseData;
            ActiveUnitData = activeUnitData;
            BuildingService = buildingService;
            SharkyUnitData = sharkyUnitData;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50)
        {
            if (unitType == UnitTypes.PROTOSS_NEXUS || unitType == UnitTypes.TERRAN_COMMANDCENTER || unitType == UnitTypes.ZERG_HATCHERY)
            {
                return GetResourceCenterLocation();
            }

            if (SharkyUnitData.TerranTypes.Contains(unitType))
            {
                return TerranBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance);
            }
            else if (SharkyUnitData.ProtossTypes.Contains(unitType))
            {
                return ProtossBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance);
            }
            else
            {
                return ZergBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance);
            }          
        }

        private Point2D GetResourceCenterLocation()
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(new Vector2(r.Unit.Pos.X, r.Unit.Pos.Y), new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (BuildingService.AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !BuildingService.Blocked(openBase.Location.X, openBase.Location.Y, 2))
                {
                    return openBase.Location;
                }
              
            }
            return null;
        }
    }
}
