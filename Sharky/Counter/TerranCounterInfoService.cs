using System.Collections.Generic;

namespace Sharky.Counter
{
    public class TerranCounterInfoService
    {
        public TerranCounterInfoService()
        {
        }

        public void PopulateTerranInfo(Dictionary<UnitTypes, CounterInfo> counterInfoData)
        {
            counterInfoData[UnitTypes.TERRAN_BATTLECRUISER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_CARRIER, 1), new CounterUnit(UnitTypes.ZERG_MUTALISK, 4), new CounterUnit(UnitTypes.TERRAN_THOR, 1) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, .34f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .34f), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .5f) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 1), new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.TERRAN_RAVEN] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 2f), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 2f), new CounterUnit(UnitTypes.TERRAN_GHOST, 2f) }
            };

            counterInfoData[UnitTypes.TERRAN_BANSHEE] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, .34f), new CounterUnit(UnitTypes.ZERG_ULTRALISK, .34f), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 2) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 2f), new CounterUnit(UnitTypes.ZERG_HYDRALISK, .5f), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 2), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 3) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 6) },
            };

            counterInfoData[UnitTypes.TERRAN_LIBERATOR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, 3), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 2) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, 2), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 1) }
            };

            counterInfoData[UnitTypes.TERRAN_VIKINGFIGHTER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_BATTLECRUISER, .34f), new CounterUnit(UnitTypes.ZERG_BROODLORD, 1), new CounterUnit(UnitTypes.PROTOSS_TEMPEST, .25f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_HYDRALISK, 1), new CounterUnit(UnitTypes.TERRAN_MARINE, .25f), new CounterUnit(UnitTypes.PROTOSS_STALKER, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 5) }
            };

            counterInfoData[UnitTypes.TERRAN_THOR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 8), new CounterUnit(UnitTypes.ZERG_MUTALISK, 4), new CounterUnit(UnitTypes.PROTOSS_STALKER, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, .1f), new CounterUnit(UnitTypes.TERRAN_MARAUDER, .25f), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 1) },
            };

            counterInfoData[UnitTypes.TERRAN_CYCLONE] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARAUDER, 2), new CounterUnit(UnitTypes.ZERG_ROACH, 3), new CounterUnit(UnitTypes.PROTOSS_ADEPT, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, .2f), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 3), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 3) }
            };

            counterInfoData[UnitTypes.TERRAN_SIEGETANK] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 8), new CounterUnit(UnitTypes.ZERG_ROACH, 4), new CounterUnit(UnitTypes.PROTOSS_STALKER, 3) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 4), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.TERRAN_BANSHEE, 2), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 2) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_VIPER, 3) },
            };

            counterInfoData[UnitTypes.TERRAN_WIDOWMINE] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, 3), new CounterUnit(UnitTypes.ZERG_ZERGLING, 10), new CounterUnit(UnitTypes.ZERG_BANELING, 5), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 4), new CounterUnit(UnitTypes.PROTOSS_ADEPT, 3) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 10), new CounterUnit(UnitTypes.ZERG_RAVAGER, 2), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 1), new CounterUnit(UnitTypes.PROTOSS_STALKER, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 6) },
            };

            counterInfoData[UnitTypes.TERRAN_HELLION] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 3), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 3), new CounterUnit(UnitTypes.ZERG_ZERGLING, 5) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 10), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 6),  new CounterUnit(UnitTypes.TERRAN_MARAUDER, 4), new CounterUnit(UnitTypes.ZERG_ROACH, 3), new CounterUnit(UnitTypes.PROTOSS_STALKER, 3) }
            };

            counterInfoData[UnitTypes.TERRAN_GHOST] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_HIGHTEMPLAR, 1), new CounterUnit(UnitTypes.TERRAN_RAVEN, 3), new CounterUnit(UnitTypes.ZERG_INFESTOR, 1) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, .2f), new CounterUnit(UnitTypes.PROTOSS_STALKER, 1), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 6) },
            };

            counterInfoData[UnitTypes.TERRAN_REAPER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 10), new CounterUnit(UnitTypes.ZERG_ROACH, 3), new CounterUnit(UnitTypes.PROTOSS_STALKER, 3), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 3) }
            };

            counterInfoData[UnitTypes.TERRAN_MARAUDER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_STALKER, 1.5f), new CounterUnit(UnitTypes.ZERG_ROACH, 1.5f), new CounterUnit(UnitTypes.TERRAN_THOR, .2f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 6), new CounterUnit(UnitTypes.ZERG_ZERGLING, .2f), new CounterUnit(UnitTypes.TERRAN_MARINE, .34f), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, .34f) }
            };

            counterInfoData[UnitTypes.TERRAN_MARINE] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_HYDRALISK, .34f), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, .25f), new CounterUnit(UnitTypes.TERRAN_MARAUDER, .34f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ULTRALISK, 8), new CounterUnit(UnitTypes.ZERG_BANELING, 4), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 8), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 8) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 10) },
            };
        }
    }
}
