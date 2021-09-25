using SC2APIProtocol;
using System.Linq;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingPlacement : IBuildingPlacement
    {
        IBuildingPlacement ProtossBuildingPlacement;
        IBuildingPlacement TerranBuildingPlacement;
        IBuildingPlacement ZergBuildingPlacement;
        ResourceCenterLocator ResourceCenterLocator;
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;
        UnitCountService UnitCountService;

        public BuildingPlacement(IBuildingPlacement protossBuildingPlacement, IBuildingPlacement terranBuildingPlacement, IBuildingPlacement zergBuildingPlacement, ResourceCenterLocator resourceCenterLocator, BaseData baseData, SharkyUnitData sharkyUnitData, MacroData macroData, UnitCountService unitCountService)
        {
            ProtossBuildingPlacement = protossBuildingPlacement;
            TerranBuildingPlacement = terranBuildingPlacement;
            ZergBuildingPlacement = zergBuildingPlacement;
            ResourceCenterLocator = resourceCenterLocator;
            BaseData = baseData;
            SharkyUnitData = sharkyUnitData;
            MacroData = macroData;
            UnitCountService = unitCountService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None)
        {
            if (unitType == UnitTypes.PROTOSS_NEXUS || unitType == UnitTypes.TERRAN_COMMANDCENTER || unitType == UnitTypes.ZERG_HATCHERY)
            {
                if (unitType == UnitTypes.TERRAN_COMMANDCENTER && MacroData.DesiredMacroCommandCenters > 0)
                {
                    if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) - BaseData.SelfBases.Count() < MacroData.DesiredMacroCommandCenters)
                    {
                        return TerranBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
                    }
                }
                return ResourceCenterLocator.GetResourceCenterLocation();
            }

            if (SharkyUnitData.TerranTypes.Contains(unitType))
            {
                return TerranBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
            }
            else if (SharkyUnitData.ProtossTypes.Contains(unitType))
            {
                return ProtossBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
            }
            else
            {
                return ZergBuildingPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
            }          
        }
    }
}
