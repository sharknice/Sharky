namespace Sharky.Builds
{
    public class AddOnSwap
    {
        public AddOnSwap(UnitTypes addon, UnitTypes builder, UnitTypes taker, bool started)
        {
            AddOnType = addon;
            DesiredAddOnBuilder = builder;
            DesiredAddOnTaker = taker;

            Started = started;
            Completed = false;
        }

        public Point2D Location { get; set; }
        public Point2D TakerLocation { get; set; }
        public Point2D AddOnLocation { get; set; }
        public UnitCommander AddOnBuilder { get; set; }
        public UnitCommander AddOnTaker { get; set; }
        public UnitCommander AddOn { get; set; }
        public UnitTypes DesiredAddOnBuilder { get; set; }
        public UnitTypes DesiredAddOnTaker { get; set; }
        public UnitTypes AddOnType { get; set; }
        public bool Started { get; set; }
        public bool Completed { get; set; }
        public bool Cancel { get; set; }
    }
}
