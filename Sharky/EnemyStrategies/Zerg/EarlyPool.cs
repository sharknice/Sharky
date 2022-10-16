using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class EarlyPool : EnemyStrategy
    {
        public EarlyPool(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        private bool earlyPool = false;

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            if (elapsedTime.TotalMinutes >= 3.0f)
            {
                return false;
            }

            if (!earlyPool)
            {
                var enemyPool = ActiveUnitData.EnemyUnits.Where(x => x.Value.Unit.UnitType == (int)UnitTypes.ZERG_SPAWNINGPOOL).Select(x => x.Value).FirstOrDefault();

                if (enemyPool is not null)
                {
                    var lingCount = UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING);
                    var enemyPoolStartedAt = UnitCountService.BuildingStarted(enemyPool);

                    // 17gas 16pool or faster -> pool starts before 50 secs, pool takes 46secs, ling takes 17 secs, +1s tolerance
                    earlyPool = enemyPoolStartedAt.TotalSeconds < 50 || (lingCount > 0 && elapsedTime.TotalSeconds < 50 + 46 + 17 + 1);
                }
            }

            return earlyPool;
        }
    }
}
