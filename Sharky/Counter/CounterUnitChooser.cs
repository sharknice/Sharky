namespace Sharky.Counter
{
    public class CounterUnitChooser
    {
        CounterInfoService CounterInfoService;

        public CounterUnitChooser(DefaultSharkyBot defaultSharkyBot, CounterInfoService counterInfoService)
        {
            CounterInfoService = counterInfoService;
        }

        public List<UnitCounterData> AssignCounters(IEnumerable<Unit> unitsToCounter, List<UnitTypes> choices)
        {
            var unitCounterData = new List<UnitCounterData>();

            foreach (var unit in unitsToCounter)
            {
                var data = new UnitCounterData(EquivalentUnit(unit));
                var counter = GetCounterUnit(unit, choices);
                if (counter != null)
                {
                    data.CounterUnits.Add(counter);
                }
                var supportCounter = GetCounterSupportUnit(unit, choices);
                if (supportCounter != null)
                {
                    data.CounterUnits.AddRange(supportCounter);
                }

                unitCounterData.Add(data);
            }

            return unitCounterData;
        }

        private Unit EquivalentUnit(Unit unit)
        {
            if (((UnitTypes)unit.UnitType).ToString().Contains("BURROWED"))
            {
                var unitType = ((UnitTypes)unit.UnitType).ToString().Replace("BURROWED", "");
                unit.UnitType = (uint)(int)Enum.Parse(typeof(UnitTypes), unitType);
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_SIEGETANKSIEGED)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_SIEGETANK;
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_THORAP)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_THOR;
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_HELLIONTANK)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_HELLION;
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_WIDOWMINEBURROWED)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_WIDOWMINE;
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_VIKINGASSAULT)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_VIKINGFIGHTER;
            }
            else if ((UnitTypes)unit.UnitType == UnitTypes.TERRAN_LIBERATORAG)
            {
                unit.UnitType = (uint)UnitTypes.TERRAN_LIBERATOR;
            }
            return unit;
        }

        CounterUnit GetCounterUnit(Unit unit, List<UnitTypes> choices)
        {
            if (CounterInfoService.CounterInfoData.ContainsKey((UnitTypes)unit.UnitType))
            {
                var data = CounterInfoService.CounterInfoData[(UnitTypes)unit.UnitType];
                if (data != null)
                {
                    return data.WeakAgainst.FirstOrDefault(u => choices.Contains(u.UnitType));
                }
            }
            return null;
        }

        IEnumerable<CounterUnit> GetCounterSupportUnit(Unit unit, List<UnitTypes> choices)
        {
            if (CounterInfoService.CounterInfoData.ContainsKey((UnitTypes)unit.UnitType))
            {
                var data = CounterInfoService.CounterInfoData[(UnitTypes)unit.UnitType];
                if (data != null && data.SupportAgainst != null)
                {
                    return data.SupportAgainst.Where(u => choices.Contains(u.UnitType));
                }
            }
            return null;
        }
    }
}
