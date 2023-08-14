namespace Sharky.EnemyStrategies
{
    public class BurrowStrategy : EnemyStrategy
    {
        SharkyUnitData SharkyUnitData;
        public BurrowStrategy(DefaultSharkyBot defaultSharkyBot) : base (defaultSharkyBot) 
        { 
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != Race.Zerg) { return false; }

            if (ActiveUnitData.EnemyUnits.Any(e => SharkyUnitData.BurrowedUnits.Contains((UnitTypes)e.Value.Unit.UnitType)))
            {
                return true;
            }

            return false;
        }
    }
}
