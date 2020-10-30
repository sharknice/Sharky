using SC2APIProtocol;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingPlacement : IBuildingPlacement
    {
        IBuildingPlacement ProtossBuildingPlacement;

        public BuildingPlacement(IBuildingPlacement protossBuildingPlacement)
        {
            ProtossBuildingPlacement = protossBuildingPlacement;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size)
        {
            if (unitType == UnitTypes.PROTOSS_NEXUS || unitType == UnitTypes.TERRAN_COMMANDCENTER || unitType == UnitTypes.ZERG_HATCHERY)
            {
                //placementLocation = GetResourceCenterLocation();
            }

            return ProtossBuildingPlacement.FindPlacement(target, unitType, size);
        }
    }
}
