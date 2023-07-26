namespace Sharky
{
    public class BaseLocation
    {
        public BaseLocation()
        {
            MineralFields = new List<Unit>();
            VespeneGeysers = new List<Unit>();
        }

        public List<Unit> MineralFields { get; set; }
        public List<MiningInfo> MineralMiningInfo { get; set; }
        public List<MiningInfo> GasMiningInfo { get; set; }
        public Point2D MineralLineLocation { get; set; }
        public Point2D MineralLineBuildingLocation { get; set; }
        public Point2D BehindMineralLineLocation { get; set; }
        public Point2D MiddleMineralLocation { get; set; }
        public List<Unit> VespeneGeysers { get; set; }
        public Point2D Location { get; set; }
        public Unit ResourceCenter { get; set; }
        public int MineralLineDefenseUnbuildableFrame { get; set; }

        public override string ToString()
        {
            return $"{Location}, {(ResourceCenter is null ? "" : ((UnitTypes)ResourceCenter.UnitType) + " " + ResourceCenter.Tag.ToString())}";
        }
    }
}
