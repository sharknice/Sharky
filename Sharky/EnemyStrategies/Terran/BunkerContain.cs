using Sharky.DefaultBot;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Terran
{
    public class BunkerContain : EnemyStrategy
    {
        TargetingData TargetingData;

        public BunkerContain(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame >= SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), u.Position) < 900 && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > 900))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
