namespace SharkyMachineLearningExample.Action
{
    public class UnitAction
    {
        public ulong UnitTag { get; set; }
        public ActionType Type { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public ulong? TargetUnitTag { get; set; }
    }
}
