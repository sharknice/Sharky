using SC2APIProtocol;
using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Zerg
{
    public class EveryZergUnit : ZergSharkyBuild
    {
        public EveryZergUnit(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
        }

        public EveryZergUnit(BuildOptions buildOptions, 
            MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData,
            ChatService chatService, UnitCountService unitCountService,
            FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService, frameToTimeConverter)
        {
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_SPAWNINGPOOL] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_SPAWNINGPOOL] = 1;
                }
                if (MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] = 2;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_EVOLUTIONCHAMBER] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_EVOLUTIONCHAMBER] = 1;
                }
                MacroData.DesiredUpgrades[Upgrades.ZERGLINGMOVEMENTSPEED] = true;
                MacroData.DesiredUpgrades[Upgrades.BURROW] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_BANELINGNEST] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_BANELINGNEST] = 1;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_ROACHWARREN] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_ROACHWARREN] = 1;
                }
                if (MacroData.DesiredMorphCounts[UnitTypes.ZERG_LAIR] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.ZERG_LAIR] = 1;
                }

                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_ZERGLING] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_ZERGLING] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_QUEEN] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_QUEEN] = 1;
                }

                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.ZERG_SPINECRAWLER] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.ZERG_SPINECRAWLER] = 1;
                }
                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.ZERG_SPORECRAWLER] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.ZERG_SPORECRAWLER] = 1;
                }

                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.ZERG_SPORECRAWLER] = 1;
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.ZERG_SPINECRAWLER] = 1;

                MacroData.DesiredDefensiveBuildingsAtEveryBase[UnitTypes.ZERG_SPORECRAWLER] = 1;
                MacroData.DesiredDefensiveBuildingsAtEveryMineralLine[UnitTypes.ZERG_SPORECRAWLER] = 1;

                MacroData.DesiredUpgrades[Upgrades.OVERLORDSPEED] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_EVOLUTIONCHAMBER) > 0)
            {
                MacroData.DesiredUpgrades[Upgrades.ZERGMELEEWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGMELEEWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGMELEEWEAPONSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGMISSILEWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGMISSILEWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGMISSILEWEAPONSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGGROUNDARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGGROUNDARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGGROUNDARMORSLEVEL3] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_ROACHWARREN) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_ROACH] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_ROACH] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_RAVAGER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_RAVAGER] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.GLIALRECONSTITUTION] = true;
                MacroData.DesiredUpgrades[Upgrades.TUNNELINGCLAWS] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_BANELINGNEST) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_BANELING] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_BANELING] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.CENTRIFICALHOOKS] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_LAIR) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_HYDRALISKDEN] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_HYDRALISKDEN] = 1;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_SPIRE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_SPIRE] = 1;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_INFESTATIONPIT] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_INFESTATIONPIT] = 1;
                }
                if (MacroData.DesiredProductionCounts[UnitTypes.ZERG_NYDUSNETWORK] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.ZERG_NYDUSNETWORK] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERSEER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERSEER] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORDTRANSPORT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORDTRANSPORT] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_INFESTATIONPIT) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_INFESTOR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_INFESTOR] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_SWARMHOSTMP] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_SWARMHOSTMP] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.INFESTORENERGYUPGRADE] = true;
                MacroData.DesiredUpgrades[Upgrades.NEURALPARASITE] = true;

                if (MacroData.DesiredMorphCounts[UnitTypes.ZERG_HIVE] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.ZERG_HIVE] = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HYDRALISKDEN) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_HYDRALISK] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_HYDRALISK] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.EVOLVEGROOVEDSPINES] = true;
                MacroData.DesiredUpgrades[Upgrades.EVOLVEMUSCULARAUGMENTS] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_SPIRE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_MUTALISK] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_MUTALISK] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_CORRUPTOR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_CORRUPTOR] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERARMORSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.ZERGFLYERWEAPONSLEVEL3] = true;

                if (MacroData.DesiredMorphCounts[UnitTypes.ZERG_GREATERSPIRE] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.ZERG_GREATERSPIRE] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_HIVE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_VIPER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_VIPER] = 1;
                }
                MacroData.DesiredUpgrades[Upgrades.ZERGLINGATTACKSPEED] = true;

                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_ULTRALISKCAVERN] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_ULTRALISKCAVERN] = 1;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_LURKERDENMP] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_LURKERDENMP] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_LURKERDENMP) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_LURKERMP] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_LURKERMP] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.LURKERRANGE] = true;
                MacroData.DesiredUpgrades[Upgrades.LURKERSPEED] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_ULTRALISKCAVERN) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_ULTRALISK] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_ULTRALISK] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.ANABOLICSYNTHESIS] = true;
                MacroData.DesiredUpgrades[Upgrades.CHITINOUSPLATING] = true;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_GREATERSPIRE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.ZERG_BROODLORD] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_BROODLORD] = 1;
                }
            }

            if (MacroData.Minerals > 500)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] <= UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY))
                {
                    MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY]++;
                }
            }
        }
    }
}
