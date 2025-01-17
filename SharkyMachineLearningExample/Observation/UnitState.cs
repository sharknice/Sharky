namespace SharkyMachineLearningExample.Observation
{
    public class UnitState
    {
        public long Tag { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Health { get; set; }
        public float Shields { get; set; }
        public float WeaponCooldown { get; set; } // Only for friendly units
    }
}
