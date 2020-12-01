using SC2APIProtocol;

namespace Sharky.Builds
{
    public interface IBuildingBuilder
    {
        Action BuildBuilding(MacroData macroData, UnitTypes unitType, BuildingTypeData unitData, Point2D generalLocation = null, bool ignoreMineralProximity = false, float maxDistance = 50);
        Action BuildAddOn(MacroData macroData, TrainingTypeData unitData);
        Action BuildGas(MacroData macroData, BuildingTypeData unitData, Unit geyser);
        Point2D GetReferenceLocation(Point2D buildLocation);
    }
}
