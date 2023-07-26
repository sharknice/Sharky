namespace Sharky.EnemyStrategies
{
    public class InvisibleAttacksSuspected : EnemyStrategy
    {
        public InvisibleAttacksSuspected(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss)
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
