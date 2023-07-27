namespace Sharky.Pathing
{
    public class MapData
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public MapCell[,] Map { get; set; }
        public string MapName { get; set; }
        public List<WallData> WallData { get; set; }
        public List<PathData> PathData { get; set; }
    }
}
