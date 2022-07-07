using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Zerg
{
    public class RoachRush : EnemyStrategy
    {
        public RoachRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            int enemyRoaches = UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH);

            if (elapsedTime.TotalMinutes > 5f)
            {
                return false;
            }

            if (enemyRoaches > 0 && elapsedTime.TotalMinutes <= 4.0)
            {
                return true;
            }

            if (enemyRoaches >= 6 && elapsedTime.TotalMinutes <= 5.0)
            {
                return true;
            }

            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) > 0 && elapsedTime.TotalMinutes < 3.2f)
            {
                return true;
            }

            if (UnitCountService.EnemyCompleted(UnitTypes.ZERG_ROACHWARREN) > 0 && elapsedTime.TotalMinutes < 3.8f)
            {
                return true;
            }

            return false;
        }
    }
}
