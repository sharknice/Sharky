namespace Sharky
{
    [Flags]
    public enum UnitClassification
    {
        None = 0,
        ArmyUnit = 1,
        Worker = 2,
        DefensiveStructure = 4,
        ProductionStructure = 8,
        ResourceCenter = 16,
        Detector = 32,
        DetectionCaster = 64,
        Cloakable = 128
    }
}
