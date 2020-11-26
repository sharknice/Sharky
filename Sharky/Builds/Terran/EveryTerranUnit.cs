using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Terran
{
    public class EveryTerranUnit : TerranSharkyBuild
    {
        public EveryTerranUnit(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {

        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 2;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.TERRAN_ENGINEERINGBAY] < 3)
                {
                    MacroData.DesiredTechCounts[UnitTypes.TERRAN_ENGINEERINGBAY] = 3;
                }
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] = 2;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.TERRAN_GHOSTACADEMY] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.TERRAN_GHOSTACADEMY] = 1;
                }

                if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] = 1;
                }

                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARINE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARINE] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_REAPER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_REAPER] = 1;
                }

                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_BARRACKSREACTOR] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_BARRACKSREACTOR] = 1;
                }
                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_BARRACKSTECHLAB] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_BARRACKSTECHLAB] = 1;
                }

                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_BUNKER] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_BUNKER] = 1;
                }
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_ENGINEERINGBAY) > 0)
            {
                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_MISSILETURRET] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_MISSILETURRET] = 1;
                }
                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_SENSORTOWER] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.TERRAN_SENSORTOWER] = 1;
                }
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.TERRAN_MISSILETURRET] = 1;
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.TERRAN_BUNKER] = 1;

                MacroData.DesiredDefensiveBuildingsAtEveryBase[UnitTypes.TERRAN_MISSILETURRET] = 1;
                MacroData.DesiredDefensiveBuildingsAtEveryMineralLine[UnitTypes.TERRAN_MISSILETURRET] = 1;

                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYARMORSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANINFANTRYWEAPONSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.HISECAUTOTRACKING] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANBUILDINGARMOR] = true;
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_BARRACKSTECHLAB) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARAUDER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARAUDER] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.SHIELDWALL] = true;
                MacroData.DesiredUpgrades[Upgrades.STIMPACK] = true;
                MacroData.DesiredUpgrades[Upgrades.PUNISHERGRENADES] = true;
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_GHOSTACADEMY) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_GHOST] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_GHOST] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.PERSONALCLOAKING] = true;
                MacroData.DesiredUpgrades[Upgrades.ENHANCEDSHOCKWAVES] = true;
            }

            if (UnitManager.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) > 1)
            {
                if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_PLANETARYFORTRESS] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.TERRAN_PLANETARYFORTRESS] = 1;
                }
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_FACTORY) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT] = 2;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.TERRAN_ARMORY] < 3)
                {
                    MacroData.DesiredTechCounts[UnitTypes.TERRAN_ARMORY] = 3;
                }

                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLION] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLION] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_WIDOWMINE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_WIDOWMINE] = 1;
                }

                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] = 1;
                }
                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYTECHLAB] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYTECHLAB] = 1;
                }
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_FACTORYTECHLAB) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_SIEGETANK] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_SIEGETANK] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_CYCLONE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_CYCLONE] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.CYCLONELOCKONDAMAGE] = true;
                MacroData.DesiredUpgrades[Upgrades.DRILLCLAWS] = true;
                MacroData.DesiredUpgrades[Upgrades.SMARTSERVOS] = true;
                MacroData.DesiredUpgrades[Upgrades.HIGHCAPACITYBARRELS] = true;
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_ARMORY) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_THOR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_THOR] = 1;
                }

                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLIONTANK] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLIONTANK] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.TERRANSHIPWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANSHIPWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANSHIPWEAPONSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.TERRANVEHICLEWEAPONSLEVEL3] = true;
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_STARPORT) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_VIKINGFIGHTER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_VIKINGFIGHTER] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MEDIVAC] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MEDIVAC] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_LIBERATOR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_LIBERATOR] = 1;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.TERRAN_FUSIONCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.TERRAN_FUSIONCORE] = 1;
                }

                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTREACTOR] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTREACTOR] = 1;
                }
                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTTECHLAB] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTTECHLAB] = 1;
                }
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_STARPORTTECHLAB) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_BANSHEE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_BANSHEE] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_RAVEN] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_RAVEN] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.RAVENCORVIDREACTOR] = true;
                MacroData.DesiredUpgrades[Upgrades.BANSHEECLOAK] = true;
                MacroData.DesiredUpgrades[Upgrades.BANSHEESPEED] = true;
            }

            if (UnitManager.Completed(UnitTypes.TERRAN_FUSIONCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_BATTLECRUISER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_BATTLECRUISER] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.BATTLECRUISERENABLESPECIALIZATIONS] = true;
                MacroData.DesiredUpgrades[Upgrades.LIBERATORAGRANGEUPGRADE] = true;
                MacroData.DesiredUpgrades[Upgrades.MEDIVACINCREASESPEEDBOOST] = true;
            }

            if (MacroData.Minerals > 500)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] <= UnitManager.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER))
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER]++;
                }
            }
        }
    }
}
