using Sharky.DefaultBot;
using System.Linq;

namespace Sharky
{
    public class EnemyAggressivityService
    {
        private EnemyAggressivityData EnemyAgresivityData { get; set; }
        private ActiveUnitData ActiveUnitData { get; set; }
        private EnemyData EnemyData { get; set; }
        private DefaultSharkyBot DefaultSharkyBot { get; set; }

        public EnemyAggressivityService(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            DefaultSharkyBot = defaultSharkyBot;

            EnemyData = defaultSharkyBot.EnemyData;
            EnemyData.EnemyAggressivityData = new EnemyAggressivityData();
            EnemyAgresivityData = EnemyData.EnemyAggressivityData;
            EnemyAgresivityData.DistanceGrid = new DistanceGrid(DefaultSharkyBot);
        }

        public void Update(int frame)
        {
            EnemyAgresivityData.DistanceGrid.Update(frame);

            var grid = EnemyAgresivityData.DistanceGrid;

            float aggressivitySum = 0;
            var unitCount = 0;

            foreach (var unit in ActiveUnitData.EnemyUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)))
            {
                int x = (int)(unit.Unit.Pos.X + 0.5f);
                int y = (int)(unit.Unit.Pos.Y + 0.5f);
                var selfDist = grid.GetDist(x, y, true, !unit.Unit.IsFlying);
                var enemyDist = grid.GetDist(x, y, false, !unit.Unit.IsFlying);

                aggressivitySum += (float)enemyDist / (float)(selfDist + enemyDist + 1);
                unitCount++;
            }

            if (unitCount == 0)
            {
                EnemyAgresivityData.ArmyAggressivity = 0;
            }
            else
            {
                EnemyAgresivityData.ArmyAggressivity = (float)aggressivitySum / (float)unitCount;
            }
        }
    }
}
