namespace Sharky
{
    public class SharkyData
    {
        public ActiveUnitData ActiveUnitData { get; set; }
        public AttackData AttackData { get; set; }
        public Dictionary<string, IEnemyStrategy> EnemyStrategies { get; set; }
        public MicroData MicroData { get; set; }
        public SharkyOptions SharkyOptions { get; set; }
    }
}
