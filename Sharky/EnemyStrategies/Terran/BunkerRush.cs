using Sharky.DefaultBot;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Terran
{
    public class BunkerRush : EnemyStrategy
    {
        TargetingData TargetingData;
        EnemyData EnemyData;

        public BunkerRush(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;

            TargetingData = defaultSharkyBot.TargetingData;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;

            EnemyData = defaultSharkyBot.EnemyData;
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), u.Position) < 900))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
