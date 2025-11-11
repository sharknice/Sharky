using Sharky.Extensions;

namespace Sharky.EnemyStrategies.Protoss
{
    public class ProxyNexus : EnemyStrategy
    {
        TargetingData TargetingData;

        public ProxyNexus(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && Vector2.Distance(TargetingData.SelfMainBasePoint.ToVector2(), u.Position) < 25))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
