namespace Sharky
{
    public class BaseData
    {
        public List<BaseLocation> BaseLocations { get; set; }
        public List<BaseLocation> SelfBases { get; set; }
        public List<BaseLocation> EnemyBaseLocations { get; set; }
        public List<BaseLocation> EnemyBases { get; set; }
        public BaseLocation MainBase { get; set; }
        public BaseLocation EnemyNaturalBase { get; set; }
    }
}
