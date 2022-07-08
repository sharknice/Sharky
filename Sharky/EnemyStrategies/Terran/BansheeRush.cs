using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Terran
{
    public class BansheeRush : EnemyStrategy
    {
        public BansheeRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran)
                return false;

            if ((frame <= SharkyOptions.FramesPerSecond * 60 * 6.5f) && UnitCountService.EnemyCount(UnitTypes.TERRAN_BANSHEE) > 0)
            {
                return true;
            }

            if ((frame <= SharkyOptions.FramesPerSecond * 60 * 6.0f) && UnitCountService.EnemyCount(UnitTypes.TERRAN_STARPORTTECHLAB) > 0 || UnitCountService.EnemyCount(UnitTypes.TERRAN_STARPORT) > 1)
            {
                return true;
            }

            return false;
        }
    }
}
