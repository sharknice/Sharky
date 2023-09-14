namespace Sharky
{
    public class RequirementService
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;

        public RequirementService(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
        }

        public bool HaveRequiredTech(UnitTypes buildingType)
        {
            throw new NotImplementedException(); // TODO:add this in the future
        }

        public bool HaveRequiredResources(UnitTypes buildingType)
        {
            throw new NotImplementedException(); // TODO:add this in the future
        }

        public bool HaveCompleted(UnitTypes buildingType)
        {
            return ActiveUnitData.SelfUnits.Values.Any(u => u.Unit.UnitType == (uint)buildingType && u.Unit.BuildProgress == 1);
        }

        public bool HaveEquivalentCompleted(UnitTypes buildingType)
        {
            var equivalentTypes = GetEquivalentTypes(buildingType);
            return ActiveUnitData.SelfUnits.Values.Any(u => equivalentTypes.Contains((UnitTypes)u.Unit.UnitType) && u.Unit.BuildProgress == 1);
        }

        public List<UnitTypes> GetEquivalentTypes(UnitTypes unitType)
        {
            var types = new List<UnitTypes> { unitType };

            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                types.Add(UnitTypes.PROTOSS_WARPGATE);
            }
            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                types.Add(UnitTypes.ZERG_HIVE);
                types.Add(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.ZERG_LAIR)
            {
                types.Add(UnitTypes.ZERG_HIVE);
            }
            else if (unitType == UnitTypes.ZERG_SPINECRAWLER)
            {
                types.Add(UnitTypes.ZERG_SPINECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.ZERG_SPORECRAWLER)
            {
                types.Add(UnitTypes.ZERG_SPORECRAWLERUPROOTED);
            }
            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                types.Add(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                types.Add(UnitTypes.TERRAN_ORBITALCOMMAND);
                types.Add(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                types.Add(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_ORBITALCOMMAND)
            {
                types.Add(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                types.Add(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                types.Add(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                types.Add(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                types.Add(UnitTypes.TERRAN_STARPORTFLYING);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                types.Add(UnitTypes.ZERG_GREATERSPIRE);
            }

            return types;
        }
    }
}
