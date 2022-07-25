using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingDroneRush : EnemyStrategy
    {
        public ZerglingDroneRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            if (frame <= SharkyOptions.FramesPerSecond * 60 * 5f
                && EnemyData.EnemyStrategies[nameof(ZerglingRush)].Detected
                && ActiveUnitData.EnemyUnits.Values.Count(u => u.Unit.UnitType == (int)UnitTypes.ZERG_DRONE) > 2)
            {
                return true;
            }

            return false;
        }
    }
}
