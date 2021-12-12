
namespace Sharky.Counter
{
    public class CounterUnit
    {
        public CounterUnit(UnitTypes unitTypes, float count)
        {
            UnitType = unitTypes;
            Count = count;
        }

        public UnitTypes UnitType { get; set; }
        public float Count { get; set; }
    }
}
