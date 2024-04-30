namespace Sharky.EnemyStrategies
{
    public class OneBase : EnemyStrategy
    {
        TargetingData TargetingData;
        BaseData BaseData;
        MapDataService MapDataService;

        bool Expired;

        public OneBase(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            MapDataService = defaultSharkyBot.MapDataService;
            Expired = false;
        }

        protected override bool Detect(int frame)
        {
            if (Expired) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            var enemyExpansions = ActiveUnitData.EnemyUnits.Values.Count(x => 
                x.UnitClassifications.HasFlag(UnitClassification.ResourceCenter)
                && x.Unit.Pos.ToVector2().DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2()) > 16.0f);

            if (enemyExpansions > 0)
            {
                Expired = true;
                return false;
            }

            if (BaseData.EnemyNaturalBase == null) { return false; }

            if (frame - MapDataService.MapData.Map[(int)BaseData.EnemyNaturalBase.Location.X, (int)BaseData.EnemyNaturalBase.Location.Y].LastFrameVisibility > 60 * 22.4f)
            {
                return false;
            }

            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg)
            {
                return elapsedTime.TotalMinutes >= 2f; // Hatch first starts before 1:00, 12pool-queen-lings-hatch starts hatch around 1:40
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                return elapsedTime.TotalMinutes >= 2.0f; // With Pylon-Gate-Assimilator Nexus is usually started around 1:25
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                return elapsedTime.TotalMinutes >= 3.0f; // With Factory before CC the CC starts before 2:30
            }

            return false;
        }
    }
}
