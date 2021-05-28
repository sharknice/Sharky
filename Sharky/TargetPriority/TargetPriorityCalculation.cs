namespace Sharky
{
    public class TargetPriorityCalculation
    {
        public TargetPriority TargetPriority { get; set; }
        public bool Overwhelm { get; set; }
        public float OverallWinnability { get; set; }
        public float AirWinnability { get; set; }
        public float GroundWinnability { get; set; }
        public int FrameCalculated { get; set; }
    }
}
