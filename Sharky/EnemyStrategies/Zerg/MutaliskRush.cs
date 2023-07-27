namespace Sharky.EnemyStrategies.Zerg
{
    public class MutaliskRush : EnemyStrategy
    {
        public MutaliskRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            if (elapsedTime.TotalMinutes <= 6.9f && UnitCountService.EnemyCount(UnitTypes.ZERG_MUTALISK) > 0)
                return true;

            if (elapsedTime.TotalMinutes < 6.5f && UnitCountService.EnemyCompleted(UnitTypes.ZERG_SPIRE) > 0)
            {
                return true;
            }

            if (elapsedTime.TotalMinutes < 5.8f && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_SPIRE) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
