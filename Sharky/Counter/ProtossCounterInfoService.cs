using System.Collections.Generic;

namespace Sharky.Counter
{
    public class ProtossCounterInfoService
    {
        public ProtossCounterInfoService()
        {
        }

        public void PopulateProtossInfo(Dictionary<UnitTypes, CounterInfo> counterInfoData)
        {
            counterInfoData[UnitTypes.PROTOSS_MOTHERSHIP] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, .2f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .2f), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .25f) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, .34f), new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.PROTOSS_CARRIER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, 4), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 5), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 3) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, .25f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .25f), new CounterUnit(UnitTypes.PROTOSS_TEMPEST, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 2), new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.PROTOSS_TEMPEST] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 2), new CounterUnit(UnitTypes.TERRAN_LIBERATOR, 2), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 1) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, .34f),  new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .5f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .34f) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.PROTOSS_ORACLE] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 2), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 1), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 1) }
            };

            counterInfoData[UnitTypes.PROTOSS_VOIDRAY] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.TERRAN_BATTLECRUISER, .34f), new CounterUnit(UnitTypes.PROTOSS_TEMPEST, .5f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, .34f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .5f), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, .5f) }
            };

            counterInfoData[UnitTypes.PROTOSS_PHOENIX] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.TERRAN_BANSHEE, 1), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .5f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.TERRAN_BATTLECRUISER, 3), new CounterUnit(UnitTypes.PROTOSS_CARRIER, 3) }
            };

            counterInfoData[UnitTypes.PROTOSS_DISRUPTOR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARAUDER, 3), new CounterUnit(UnitTypes.ZERG_HYDRALISK, 3), new CounterUnit(UnitTypes.PROTOSS_STALKER, 3) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ULTRALISK, 2), new CounterUnit(UnitTypes.ZERG_BROODLORD, 2), new CounterUnit(UnitTypes.TERRAN_THOR, 2), new CounterUnit(UnitTypes.PROTOSS_TEMPEST, 3) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.PROTOSS_COLOSSUS] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 8), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 5), new CounterUnit(UnitTypes.ZERG_ZERGLING, 10) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 1), new CounterUnit(UnitTypes.ZERG_CORRUPTOR, .34f), new CounterUnit(UnitTypes.ZERG_MUTALISK, .2f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .34f), new CounterUnit(UnitTypes.PROTOSS_TEMPEST, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_VIPER, 1) },
            };

            counterInfoData[UnitTypes.PROTOSS_IMMORTAL] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ROACH, 3), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 2), new CounterUnit(UnitTypes.PROTOSS_STALKER, 3) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 1), new CounterUnit(UnitTypes.ZERG_MUTALISK, .34f), new CounterUnit(UnitTypes.ZERG_ZERGLING, .2f), new CounterUnit(UnitTypes.TERRAN_MARINE, .2f), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, .25f) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 3), new CounterUnit(UnitTypes.ZERG_VIPER, 3) },
            };

            counterInfoData[UnitTypes.PROTOSS_WARPPRISM] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1) }
            };

            counterInfoData[UnitTypes.PROTOSS_OBSERVER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_DARKTEMPLAR, 5), new CounterUnit(UnitTypes.TERRAN_BANSHEE, 5), new CounterUnit(UnitTypes.ZERG_ROACH, 10) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 4), new CounterUnit(UnitTypes.ZERG_MUTALISK, 4), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 5) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 5) },
            };

            counterInfoData[UnitTypes.PROTOSS_ARCHON] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 5), new CounterUnit(UnitTypes.ZERG_MUTALISK, 4), new CounterUnit(UnitTypes.PROTOSS_ADEPT, 5) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_HYDRALISK, .25f), new CounterUnit(UnitTypes.TERRAN_THOR, 2), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 2) }
            };

            counterInfoData[UnitTypes.PROTOSS_DARKTEMPLAR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 5), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 4) },
            };

            counterInfoData[UnitTypes.PROTOSS_HIGHTEMPLAR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_HYDRALISK, 5), new CounterUnit(UnitTypes.PROTOSS_SENTRY, 8), new CounterUnit(UnitTypes.TERRAN_MARINE, 8) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ROACH, 1), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 4), new CounterUnit(UnitTypes.TERRAN_GHOST, 1) }
            };

            counterInfoData[UnitTypes.PROTOSS_ADEPT] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, 4), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 1), new CounterUnit(UnitTypes.TERRAN_MARINE, 2) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 1), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 1), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.ZERG_ROACH, 1), new CounterUnit(UnitTypes.PROTOSS_STALKER, 1), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 1) }
            };

            counterInfoData[UnitTypes.PROTOSS_STALKER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.TERRAN_REAPER, 3), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .25f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, .2f), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 1.5f), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 3) }
            };

            counterInfoData[UnitTypes.PROTOSS_SENTRY] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 3), new CounterUnit(UnitTypes.ZERG_HYDRALISK, 1), new CounterUnit(UnitTypes.PROTOSS_STALKER, 2), new CounterUnit(UnitTypes.TERRAN_REAPER, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 5) },
            };

            counterInfoData[UnitTypes.PROTOSS_ZEALOT] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, 4), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, .34f), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 2) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 5), new CounterUnit(UnitTypes.ZERG_MUTALISK, 3), new CounterUnit(UnitTypes.ZERG_ROACH, 1), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 8), new CounterUnit(UnitTypes.TERRAN_HELLION, 1.5f) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 8) },
            };
        }
    }
}
