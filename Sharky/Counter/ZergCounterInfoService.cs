namespace Sharky.Counter
{
    public class ZergCounterInfoService
    {
        public ZergCounterInfoService()
        {
        }

        public void PopulateZergInfo(Dictionary<UnitTypes, CounterInfo> counterInfoData)
        {
            counterInfoData[UnitTypes.ZERG_BROODLORD] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_HYDRALISK, 3), new CounterUnit(UnitTypes.PROTOSS_STALKER, 2), new CounterUnit(UnitTypes.TERRAN_MARINE, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, 2), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 1) }
            };

            counterInfoData[UnitTypes.ZERG_VIPER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 3), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 1), new CounterUnit(UnitTypes.ZERG_HYDRALISK, 5) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.ZERG_MUTALISK, .5f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .5f), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, .5f) }
            };

            counterInfoData[UnitTypes.ZERG_CORRUPTOR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_BATTLECRUISER, .25f), new CounterUnit(UnitTypes.ZERG_MUTALISK, 2), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 1.5f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 1), new CounterUnit(UnitTypes.ZERG_HYDRALISK, 1), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 2), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, 2) }
            };

            counterInfoData[UnitTypes.ZERG_MUTALISK] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, .5f), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, .5f), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .5f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 2), new CounterUnit(UnitTypes.PROTOSS_PHOENIX, 2), new CounterUnit(UnitTypes.TERRAN_LIBERATOR, 3), new CounterUnit(UnitTypes.TERRAN_THOR, 4) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 5) }
            };

            counterInfoData[UnitTypes.ZERG_OVERSEER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_CORRUPTOR, 5), new CounterUnit(UnitTypes.ZERG_MUTALISK, 5), new CounterUnit(UnitTypes.PROTOSS_STALKER, 5), new CounterUnit(UnitTypes.TERRAN_VIKINGFIGHTER, 5) }
            };

            counterInfoData[UnitTypes.ZERG_ULTRALISK] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 6), new CounterUnit(UnitTypes.ZERG_ZERGLING, 10), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 5f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 1), new CounterUnit(UnitTypes.TERRAN_GHOST, .5f), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 1) }
            };

            counterInfoData[UnitTypes.ZERG_SWARMHOSTMP] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 4), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.TERRAN_BANSHEE, 1), new CounterUnit(UnitTypes.PROTOSS_STALKER, .5f) }
            };

            counterInfoData[UnitTypes.ZERG_INFESTOR] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 1), new CounterUnit(UnitTypes.TERRAN_MARINE, 10), new CounterUnit(UnitTypes.ZERG_MUTALISK, 5) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ULTRALISK, 4), new CounterUnit(UnitTypes.TERRAN_GHOST, 1), new CounterUnit(UnitTypes.PROTOSS_HIGHTEMPLAR, 1) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 5) }
            };

            counterInfoData[UnitTypes.ZERG_LURKERMP] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 5), new CounterUnit(UnitTypes.ZERG_HYDRALISK, 3), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 4), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 3), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 2), new CounterUnit(UnitTypes.PROTOSS_DISRUPTOR, 2) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_OVERSEER, 5) }
            };

            counterInfoData[UnitTypes.ZERG_HYDRALISK] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_BANSHEE, .25f), new CounterUnit(UnitTypes.ZERG_MUTALISK, 1), new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .2f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_ZERGLING, .25f), new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 4), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 5) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 5) }
            };

            counterInfoData[UnitTypes.ZERG_RAVAGER] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_SIEGETANK, 1), new CounterUnit(UnitTypes.ZERG_LURKERMP, 1), new CounterUnit(UnitTypes.PROTOSS_SENTRY, 1) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 3), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 4), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 2), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 4) }
            };

            counterInfoData[UnitTypes.ZERG_ROACH] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_HELLION, 3), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 3), new CounterUnit(UnitTypes.ZERG_ZERGLING, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 6), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 4), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 2), new CounterUnit(UnitTypes.PROTOSS_IMMORTAL, 4) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 8) }
            };

            counterInfoData[UnitTypes.ZERG_BANELING] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARINE, 4), new CounterUnit(UnitTypes.ZERG_ZERGLING, 8), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, 4) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 8), new CounterUnit(UnitTypes.ZERG_ROACH, 4), new CounterUnit(UnitTypes.TERRAN_MARAUDER, 4), new CounterUnit(UnitTypes.PROTOSS_STALKER, 4) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 10) }
            };

            counterInfoData[UnitTypes.ZERG_ZERGLING] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.TERRAN_MARAUDER, .25f), new CounterUnit(UnitTypes.PROTOSS_STALKER, .2f), new CounterUnit(UnitTypes.ZERG_HYDRALISK, .2f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 12), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 12), new CounterUnit(UnitTypes.ZERG_BANELING, 6), new CounterUnit(UnitTypes.TERRAN_HELLION, 4), new CounterUnit(UnitTypes.PROTOSS_COLOSSUS, 10) },
                SupportAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_INFESTOR, 15) }
            };

            counterInfoData[UnitTypes.ZERG_QUEEN] = new CounterInfo
            {
                StrongAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.PROTOSS_VOIDRAY, .5f), new CounterUnit(UnitTypes.TERRAN_HELLION, 4f), new CounterUnit(UnitTypes.ZERG_MUTALISK, 2f) },
                WeakAgainst = new List<CounterUnit> { new CounterUnit(UnitTypes.ZERG_BROODLORD, 3), new CounterUnit(UnitTypes.ZERG_ULTRALISK, 5), new CounterUnit(UnitTypes.ZERG_ZERGLING, .15f), new CounterUnit(UnitTypes.PROTOSS_ZEALOT, .25f), new CounterUnit(UnitTypes.TERRAN_MARINE, .2f)}
            };
        }
    }
}
