using Sharky.Helper;

namespace Sharky.Builds
{
    public class ProxyData
    {
        public ProxyData(Point2D location, MacroData macroData, bool enabled = false)
        {
            Location = location;
            Enabled = true;
            MaximumBuildingDistance = 15;
            DesiredPylons = 0;

            DesiredMorphCounts = new Dictionary<UnitTypes, ValueRange>();
            foreach (var productionType in macroData.Morphs)
            {
                DesiredMorphCounts[productionType] = 0;
            }

            DesiredProductionCounts = new Dictionary<UnitTypes, ValueRange>();
            foreach (var productionType in macroData.Production)
            {
                DesiredProductionCounts[productionType] = 0;
            }

            DesiredTechCounts = new Dictionary<UnitTypes, ValueRange>();
            foreach (var techType in macroData.Tech)
            {
                DesiredTechCounts[techType] = 0;
            }

            DesiredAddOnCounts = new Dictionary<UnitTypes, ValueRange>();
            foreach (var techType in macroData.AddOns)
            {
                DesiredAddOnCounts[techType] = 0;
            }

            DesiredDefensiveBuildingsCounts = new Dictionary<UnitTypes, ValueRange>();
            foreach (var defensiveBuildingsType in macroData.DefensiveBuildings)
            {
                DesiredDefensiveBuildingsCounts[defensiveBuildingsType] = 0;
            }

            Enabled = enabled;
        }

        public bool Enabled { get; set; }
        public Point2D Location { get; set; }
        public float MaximumBuildingDistance { get; set; }

        public Dictionary<UnitTypes, ValueRange> DesiredProductionCounts;
        public Dictionary<UnitTypes, ValueRange> DesiredMorphCounts;
        public Dictionary<UnitTypes, ValueRange> DesiredTechCounts;
        public Dictionary<UnitTypes, ValueRange> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, ValueRange> DesiredAddOnCounts;

        public ValueRange DesiredPylons;

        // TODO: hard coded data
        public HardCodedBuildingData HardCodedBuildingData { get; set; }
    }
}
