namespace Sharky.EnemyStrategies.Protoss
{
    public class FastStargate : EnemyStrategy
    {
        public FastStargate(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss || EnemyData.EnemyStrategies[nameof(ProxyStargate)].Detected) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 4f && UnitCountService.EnemyCount(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 5.5f && UnitCountService.EnemyCount(UnitTypes.PROTOSS_VOIDRAY) > 0)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 5.5f && UnitCountService.EnemyCount(UnitTypes.PROTOSS_ORACLE) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
