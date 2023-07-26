namespace Sharky.Builds
{
    public interface IBuildingBuilder
    {
        bool HasRoomForAddon(Unit building);
        List<SC2Action> BuildAddOn(MacroData macroData, TrainingTypeData unitData, Point2D location = null, float maxDistance = 50);
        List<SC2Action> BuildGas(MacroData macroData, BuildingTypeData unitData, Unit geyser);
        List<SC2Action> BuildBuilding(MacroData macroData, UnitTypes unitType, BuildingTypeData unitData, Point2D generalLocation = null, bool ignoreMineralProximity = false, float maxDistance = 50, List<UnitCommander> workerPool = null, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool allowBlockBase = false);
        Point2D GetReferenceLocation(Point2D buildLocation);
    }
}
