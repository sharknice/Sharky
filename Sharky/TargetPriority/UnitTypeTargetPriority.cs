namespace Sharky
{
    public class UnitTypeTargetPriority : IComparer<UnitTypes>
    {
        protected IList<UnitTypes> orderedTypes { get; set; }

        public UnitTypeTargetPriority()
        {
            orderedTypes = new List<UnitTypes>() {
                UnitTypes.PROTOSS_PYLON,
                UnitTypes.PROTOSS_CYBERNETICSCORE,
                UnitTypes.PROTOSS_NEXUS,
                UnitTypes.ZERG_HIVE,
                UnitTypes.ZERG_LAIR,
                UnitTypes.ZERG_SPAWNINGPOOL,
                UnitTypes.ZERG_ROACHWARREN,
                UnitTypes.ZERG_GREATERSPIRE,
                UnitTypes.ZERG_SPIRE,
                UnitTypes.ZERG_HATCHERY,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.TERRAN_ORBITALCOMMANDFLYING,
                UnitTypes.TERRAN_COMMANDCENTER,
                UnitTypes.TERRAN_COMMANDCENTERFLYING
            };
        }

        public int Compare(UnitTypes x, UnitTypes y)
        {
            var xIndex = orderedTypes.IndexOf(x);
            var yIndex = orderedTypes.IndexOf(y);

            if (xIndex == -1) { xIndex = 999; }
            if (yIndex == -1) { yIndex = 999; }

            return xIndex.CompareTo(yIndex);
        }
    };
}
