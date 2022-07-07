using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class OneBase : EnemyStrategy
    {
        TargetingData TargetingData;
        BaseData BaseData;
        MapDataService MapDataService;

        public OneBase(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            MapDataService = defaultSharkyBot.MapDataService;
        }

        protected override bool Detect(int frame)
        {
            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            var enemyExpansions = ActiveUnitData.EnemyUnits.Values.Count(x => 
                x.UnitClassifications.Contains(UnitClassification.ResourceCenter)
                && x.Unit.Pos.ToVector2().DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2()) > 16.0f);

            if (MapDataService.LastFrameVisibility(BaseData.EnemyNaturalBase.Location) == 0)
                return false;

            if (enemyExpansions > 0)
                return false;

            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg)
            {
                return elapsedTime.TotalMinutes >= 2.5f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                return elapsedTime.TotalMinutes >= 3f;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                return elapsedTime.TotalMinutes >= 3.5f;
            }

            return false;
        }
    }
}
