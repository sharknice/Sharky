using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Terran
{
    public class SuspectedTerranProxy : EnemyStrategy
    {
        public SuspectedTerranProxy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 2 * 60)
            {
                return false;
            }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) < 2 && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_BARRACKS) < 1 && UnitCountService.EnemyCount(UnitTypes.TERRAN_REFINERY) > 1)
            {
                return true;
            }

            return false;
        }
    }
}
