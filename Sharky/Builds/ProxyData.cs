using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public class ProxyData
    {
        public ProxyData(Point2D location, MacroData macroData)
        {
            Location = location;
            Enabled = true;
            MaximumBuildingDistance = 15;
            DesiredPylons = 0;

            DesiredMorphCounts = new Dictionary<UnitTypes, int>();
            foreach (var productionType in macroData.Morphs)
            {
                DesiredMorphCounts[productionType] = 0;
            }

            DesiredProductionCounts = new Dictionary<UnitTypes, int>();
            foreach (var productionType in macroData.Production)
            {
                DesiredProductionCounts[productionType] = 0;
            }

            DesiredTechCounts = new Dictionary<UnitTypes, int>();
            foreach (var techType in macroData.Tech)
            {
                DesiredTechCounts[techType] = 0;
            }

            DesiredAddOnCounts = new Dictionary<UnitTypes, int>();
            foreach (var techType in macroData.AddOns)
            {
                DesiredAddOnCounts[techType] = 0;
            }

            DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, int>();
            foreach (var defensiveBuildingsType in macroData.DefensiveBuildings)
            {
                DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;
            }
        }

        public bool Enabled { get; set; }
        public Point2D Location { get; set; }
        public float MaximumBuildingDistance { get; set; }

        public Dictionary<UnitTypes, int> DesiredProductionCounts;
        public Dictionary<UnitTypes, int> DesiredMorphCounts;
        public Dictionary<UnitTypes, int> DesiredTechCounts;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, int> DesiredAddOnCounts;

        public int DesiredPylons;
    }
}
