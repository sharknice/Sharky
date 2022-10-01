namespace Sharky.MicroTasks.Zerg
{
    public class InjectData
    {
        public UnitCommander Queen { get; set; }
        public UnitCommander Hatchery { get; set; }

        public override string ToString()
        {
            return $"{Hatchery?.UnitCalculation.Unit.Tag} : {Queen?.UnitCalculation.Unit.Tag}";
        }
    }
}
