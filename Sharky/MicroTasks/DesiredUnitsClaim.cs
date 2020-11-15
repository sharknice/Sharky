namespace Sharky.MicroTasks
{
    public class DesiredUnitsClaim
    {
        public DesiredUnitsClaim(UnitTypes unitType, int count)
        {
            UnitType = unitType;
            Count = count;
        }

        public UnitTypes UnitType { get; set; }
        public int Count { get; set; }
    }
}
