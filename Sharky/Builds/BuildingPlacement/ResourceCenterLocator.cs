using SC2APIProtocol;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ResourceCenterLocator
    {
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        BuildingService BuildingService;

        public ResourceCenterLocator(ActiveUnitData activeUnitData, BaseData baseData, BuildingService buildingService)
        {
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            BuildingService = buildingService;
        }

        public Point2D GetResourceCenterLocation(bool canHaveCreep)
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25 || r.Unit.Orders.Any(o => o.TargetWorldSpacePos != null && o.TargetWorldSpacePos.X == b.Location.X && o.TargetWorldSpacePos.Y == b.Location.Y)));

            foreach (var openBase in openBases)
            {
                if (BuildingService.AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !BuildingService.Blocked(openBase.Location.X, openBase.Location.Y, 2, 0))
                {
                    if (!canHaveCreep && BuildingService.HasAnyCreep(openBase.Location.X, openBase.Location.Y, 2.5f / 2.0f))
                    {
                        continue;
                    }
                    return openBase.Location;
                }
            }
            return null;
        }
    }
}
