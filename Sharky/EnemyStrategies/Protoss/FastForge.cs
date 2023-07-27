namespace Sharky.EnemyStrategies.Protoss
{
    public class FastForge : EnemyStrategy
    {
        public FastForge(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (int)UnitTypes.PROTOSS_FORGE))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
