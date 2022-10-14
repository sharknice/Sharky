using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Linq;

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
                x.UnitClassifications.Contains(UnitClassification.ResourceCenter)
                && x.Unit.Pos.ToVector2().DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2()) > 16.0f);

            if (MapDataService.LastFrameVisibility(BaseData.EnemyNaturalBase.Location) == 0)
                return false;

            if (enemyExpansions > 0)
            {
                return false;
            }

            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg)
            {
                return elapsedTime.TotalMinutes >= 2.5f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                return elapsedTime.TotalMinutes >= 3.5f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                return elapsedTime.TotalMinutes >= 3.5f;
            }

            if (enemyExpansions > 0 && elapsedTime.TotalMinutes >= 3.5f)
            {
                Expired = false;
            }

            return false;
        }
    }
}
