namespace Sharky.EnemyStrategies.Protoss
{
    public class GasSteal : EnemyStrategy
    {
        BaseData BaseData;

        public GasSteal(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            BaseData = defaultSharkyBot.BaseData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 60 * 5) { return false; }

            if (BaseData.MainBase?.VespeneGeysers == null) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_ASSIMILATOR && BaseData.MainBase.VespeneGeysers.Any(g => g.Pos.X == u.Position.X && g.Pos.Y == u.Position.Y)))
            {
                return true;
            }

            return false;
        }
    }
}
