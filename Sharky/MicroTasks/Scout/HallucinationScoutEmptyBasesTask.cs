using SC2APIProtocol;
using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class HallucinationScoutEmptyBasesTask : HallucinationScoutTask
    {
        protected ActiveUnitData ActiveUnitData;

        public HallucinationScoutEmptyBasesTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
            : base(defaultSharkyBot.TargetingData, defaultSharkyBot.BaseData, defaultSharkyBot.MicroTaskData, enabled, priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
        }

        protected override void GetScoutLocations()
        {
            ScoutLocations = new List<Point2D>();

            foreach (var baseLocation in BaseData.EnemyBaseLocations.Where(b => !ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && Vector2.DistanceSquared(e.Value.Position, new Vector2(b.Location.X, b.Location.Y)) < 50) && !ActiveUnitData.SelfUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && Vector2.DistanceSquared(e.Value.Position, new Vector2(b.Location.X, b.Location.Y)) < 50)))
            {
                ScoutLocations.Add(baseLocation.MineralLineLocation);
            }
            if (ScoutLocations.Count() == 0)
            {
                ScoutLocations.AddRange(BaseData.EnemyBaseLocations.Select(b => b.MineralLineLocation));
            }
            ScoutLocationIndex = 0;
        }
    }
}
