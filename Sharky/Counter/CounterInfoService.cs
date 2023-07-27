namespace Sharky.Counter
{
    public class CounterInfoService
    {
        public Dictionary<UnitTypes, CounterInfo> CounterInfoData { get; private set; }

        public CounterInfoService(ZergCounterInfoService zergCounterInfoService, TerranCounterInfoService terranCounterInfoService, ProtossCounterInfoService protossCounterInfoService)
        {
            CounterInfoData = new Dictionary<UnitTypes, CounterInfo>();

            zergCounterInfoService.PopulateZergInfo(CounterInfoData);
            protossCounterInfoService.PopulateProtossInfo(CounterInfoData);
            terranCounterInfoService.PopulateTerranInfo(CounterInfoData);
        }
    }
}
