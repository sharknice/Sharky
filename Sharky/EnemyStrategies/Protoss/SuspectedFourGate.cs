using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.EnemyStrategies.Protoss
{
    public class SuspectedFourGate : EnemyStrategy
    {
        MapDataService MapDataService;
        BaseData BaseData;

        public SuspectedFourGate(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 4 * 60) { return false; }

            if (MapDataService.SelfVisible(BaseData.EnemyBaseLocations.FirstOrDefault().Location))
            {
                if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_NEXUS) > 1) { return false; }

                if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 3 && UnitCountService.EnemyCount(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
                {
                    return true;
                }
            }    

            return false;
        }
    }
}
