using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.EnemyStrategies
{
    public class TwoBase : EnemyStrategy
    {
        private TargetingData TargetingData;
        private MapDataService MapDataService;
        private BaseData BaseData;

        bool Expired;

        public TwoBase(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
            Expired = false;
        }

        protected override bool Detect(int frame)
        {
            if (Expired) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            var enemyExpansions = ActiveUnitData.EnemyUnits.Values.Count(x => x.UnitClassifications.Contains(UnitClassification.ResourceCenter) 
                && x.Unit.Pos.ToVector2().DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2()) > 16.0f);

            if (MapDataService.LastFrameVisibility(BaseData.EnemyNaturalBase.Location) == 0)
            {
                return false;
            }

            if (enemyExpansions > 1)
            {
                Expired = true;
                return false;
            }

            if (enemyExpansions == 0)
            {
                return false;
            }

            // If we havent scouted third
            if (BaseData.EnemyBaseLocations.Count() >= 3 && MapDataService.LastFrameVisibility(BaseData.EnemyBaseLocations.Skip(2).First().Location) == 0)
            {
                return false;
            }

            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg)
            {
                return elapsedTime.TotalMinutes >= 4f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                return elapsedTime.TotalMinutes >= 6f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                return elapsedTime.TotalMinutes >= 7f;
            }
            else
            {
                return false;
            }
        }
    }
}
