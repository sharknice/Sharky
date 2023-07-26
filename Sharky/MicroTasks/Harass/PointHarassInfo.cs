namespace Sharky.MicroTasks.Harass
{
    public class PointHarassInfo
    {
        public Point2D Location { get; set; }
        public int LastClearedFrame { get; set; }
        public int LastDefendedFrame { get; set; }
        public int LastPathFailedFrame { get; set; }
        public List<UnitCommander> Harassers { get; set; }
    }
}
