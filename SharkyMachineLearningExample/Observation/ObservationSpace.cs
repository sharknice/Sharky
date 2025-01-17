namespace SharkyMachineLearningExample.Observation
{
    public class ObservationSpace
    {
        public List<UnitState> FriendlyUnits { get; set; }
        public List<UnitState> EnemyUnits { get; set; }
        public float MapWidth { get; set; }
        public float MapHeight { get; set; }
        public bool[,] WalkableArea { get; set; }
    }
}
