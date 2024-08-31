namespace Sharky.Concaves
{
    public class ConcaveGroupData
    {
        public ConcaveGroupData() 
        { 
            UnitCommanders = new List<UnitCommander> { };
            GroundConcavePoints = new Dictionary<ulong, Vector2>();
            AirConcavePoints = new Dictionary<ulong, Vector2>();
            HybridConcavePoints = new Dictionary<ulong, Vector2>();
            ConvergePoint = null;
        }

        public List<UnitCommander> UnitCommanders { get; set; }
        public Dictionary<ulong, Vector2> GroundConcavePoints { get; set; }
        public Dictionary<ulong, Vector2> AirConcavePoints { get; set; }
        public Dictionary<ulong, Vector2> HybridConcavePoints { get; set; }
        public Point2D ConvergePoint { get; set; }
        public UnitCalculation Threat {  get; set; }
    }
}
