using System.Collections.Generic;
using System.Linq;

namespace Sharky.TypeData
{
    public class UpgradeDataService
    {
        public Dictionary<Upgrades, TrainingTypeData> UpgradeData()
        {
            var upgradeData = ProtossUpgradeData();
            TerranUpgradeData().ToList().ForEach(x => upgradeData.Add(x.Key, x.Value));
            ZergUpgradeData().ToList().ForEach(x => upgradeData.Add(x.Key, x.Value));
            return upgradeData;
        }

        public Dictionary<Upgrades, TrainingTypeData> ProtossUpgradeData()
        {
            return new Dictionary<Upgrades, TrainingTypeData>
            {             
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL1 } },
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL2 } },
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL3 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL1 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL2 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL3 } },
                { Upgrades.PROTOSSSHIELDSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL1 } },
                { Upgrades.PROTOSSSHIELDSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL2 } },
                { Upgrades.PROTOSSSHIELDSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL3 } },

                { Upgrades.WARPGATERESEARCH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 50, Gas = 50, Ability = Abilities.RESEARCH_WARPGATE } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL1 } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL2 } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL3 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL1 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL2 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL3 } },

                { Upgrades.GRAVITICDRIVE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GRAVITICDRIVE } },
                { Upgrades.EXTENDEDTHERMALLANCE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_EXTENDEDTHERMALLANCE } },
                { Upgrades.OBSERVERGRAVITICBOOSTER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GRAVITICBOOSTER } },

                { Upgrades.PSISTORMTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TEMPLARARCHIVE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PSISTORM } },

                { Upgrades.DARKTEMPLARBLINKUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_DARKSHRINE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_SHADOWSTRIKE } },

                { Upgrades.BLINKTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BLINK } },
                { Upgrades.CHARGE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_CHARGE } },
                { Upgrades.ADEPTPIERCINGATTACK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_ADEPTRESONATINGGLAIVES } },

                { Upgrades.TECTONICDESTABILIZERS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_TECTONICDESTABILIZERS } },
                { Upgrades.PHOENIXRANGEUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PHOENIXANIONPULSECRYSTALS } },
                { Upgrades.VOIDRAYSPEEDUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_VOIDRAYSPEEDUPGRADE } },
            };
        }

        public Dictionary<Upgrades, TrainingTypeData> TerranUpgradeData()
        {
            return new Dictionary<Upgrades, TrainingTypeData>
            {
                { Upgrades.TERRANINFANTRYWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TERRANINFANTRYWEAPONSLEVEL1 } },
                { Upgrades.TERRANINFANTRYWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_TERRANINFANTRYWEAPONSLEVEL2 } },
                { Upgrades.TERRANINFANTRYWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_TERRANINFANTRYWEAPONSLEVEL3 } },
                { Upgrades.TERRANINFANTRYARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TERRANINFANTRYARMORLEVEL1 } },
                { Upgrades.TERRANINFANTRYARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_TERRANINFANTRYARMORLEVEL2 } },
                { Upgrades.TERRANINFANTRYARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_TERRANINFANTRYARMORLEVEL3 } },
                { Upgrades.HISECAUTOTRACKING, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_HISECAUTOTRACKING } },
                { Upgrades.TERRANBUILDINGARMOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_TERRANSTRUCTUREARMORUPGRADE } },

                { Upgrades.PERSONALCLOAKING, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_GHOSTACADEMY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PERSONALCLOAKING } },
                { Upgrades.ENHANCEDSHOCKWAVES, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_GHOSTACADEMY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ENHANCEDSHOCKWAVES } },

                { Upgrades.TERRANVEHICLEWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TERRANVEHICLEWEAPONSLEVEL1 } },
                { Upgrades.TERRANVEHICLEWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_TERRANVEHICLEWEAPONSLEVEL2 } },
                { Upgrades.TERRANVEHICLEWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_TERRANVEHICLEWEAPONSLEVEL3 } },
                { Upgrades.TERRANSHIPWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TERRANSHIPWEAPONSLEVEL1 } },
                { Upgrades.TERRANSHIPWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_TERRANSHIPWEAPONSLEVEL2 } },
                { Upgrades.TERRANSHIPWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_TERRANSHIPWEAPONSLEVEL3 } },
                { Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TERRANVEHICLEANDSHIPPLATINGLEVEL1 } },
                { Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_TERRANVEHICLEANDSHIPPLATINGLEVEL2 } },
                { Upgrades.TERRANVEHICLEANDSHIPARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ARMORY }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_TERRANVEHICLEANDSHIPPLATINGLEVEL3 } },

                { Upgrades.BATTLECRUISERENABLESPECIALIZATIONS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FUSIONCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BATTLECRUISERWEAPONREFIT } },
                { Upgrades.LIBERATORAGRANGEUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FUSIONCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BALLISTICRANGE } },
                { Upgrades.MEDIVACINCREASESPEEDBOOST, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FUSIONCORE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_RAPIDREIGNITIONSYSTEM } },

                { Upgrades.SHIELDWALL, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_COMBATSHIELD } },
                { Upgrades.STIMPACK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_STIMPACK } },
                { Upgrades.PUNISHERGRENADES, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 50, Gas = 50, Ability = Abilities.RESEARCH_CONCUSSIVESHELLS } },

                { Upgrades.HIGHCAPACITYBARRELS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_INFERNALPREIGNITER } },
                { Upgrades.CYCLONELOCKONDAMAGE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_CYCLONELOCKONDAMAGE } },
                { Upgrades.DRILLCLAWS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 75, Gas = 75, Ability = Abilities.RESEARCH_DRILLINGCLAWS } },
                { Upgrades.SMARTSERVOS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_SMARTSERVOS } },

                { Upgrades.RAVENCORVIDREACTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORTTECHLAB }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_RAVENCORVIDREACTOR } },
                { Upgrades.BANSHEECLOAK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORTTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_BANSHEECLOAKINGFIELD } },
                { Upgrades.BANSHEESPEED, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORTTECHLAB }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BANSHEEHYPERFLIGHTROTORS } },
            };
        }

        public Dictionary<Upgrades, TrainingTypeData> ZergUpgradeData()
        {
            return new Dictionary<Upgrades, TrainingTypeData>
            {
                { Upgrades.BURROW, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HATCHERY, UnitTypes.ZERG_LAIR, UnitTypes.ZERG_HIVE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_BURROW } },
                { Upgrades.OVERLORDSPEED, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HATCHERY, UnitTypes.ZERG_LAIR, UnitTypes.ZERG_HIVE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PNEUMATIZEDCARAPACE } },

                { Upgrades.ZERGLINGMOVEMENTSPEED, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPAWNINGPOOL }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_ZERGLINGMETABOLICBOOST } },
                { Upgrades.ZERGLINGATTACKSPEED, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPAWNINGPOOL }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_ZERGLINGADRENALGLANDS } },

                { Upgrades.ZERGMELEEWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_ZERGMELEEWEAPONSLEVEL1 } },
                { Upgrades.ZERGMELEEWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ZERGMELEEWEAPONSLEVEL2 } },
                { Upgrades.ZERGMELEEWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_ZERGMELEEWEAPONSLEVEL3 } },
                { Upgrades.ZERGMISSILEWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_ZERGMISSILEWEAPONSLEVEL1 } },
                { Upgrades.ZERGMISSILEWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ZERGMISSILEWEAPONSLEVEL2 } },
                { Upgrades.ZERGMISSILEWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_ZERGMISSILEWEAPONSLEVEL3 } },
                { Upgrades.ZERGGROUNDARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ZERGGROUNDARMORLEVEL1 } },
                { Upgrades.ZERGGROUNDARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_ZERGGROUNDARMORLEVEL2 } },
                { Upgrades.ZERGGROUNDARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_EVOLUTIONCHAMBER }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_ZERGGROUNDARMORLEVEL3 } },

                { Upgrades.GLIALRECONSTITUTION, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_ROACHWARREN }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GLIALREGENERATION } },
                { Upgrades.TUNNELINGCLAWS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_ROACHWARREN }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_TUNNELINGCLAWS } },

                { Upgrades.CENTRIFICALHOOKS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_BANELINGNEST }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_CENTRIFUGALHOOKS } },

                { Upgrades.INFESTORENERGYUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_INFESTATIONPIT }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PATHOGENGLANDS } },
                { Upgrades.NEURALPARASITE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_INFESTATIONPIT }, Minerals = 150, Gas = 150, Ability = Abilities.EFFECT_NEURALPARASITE } },

                { Upgrades.EVOLVEGROOVEDSPINES, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HYDRALISKDEN }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GROOVEDSPINES } },
                { Upgrades.EVOLVEMUSCULARAUGMENTS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HYDRALISKDEN }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_MUSCULARAUGMENTS } },

                { Upgrades.ZERGFLYERARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ZERGFLYERARMORLEVEL1 } },
                { Upgrades.ZERGFLYERARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_ZERGFLYERARMORLEVEL2 } },
                { Upgrades.ZERGFLYERARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_ZERGFLYERARMORLEVEL3 } },
                { Upgrades.ZERGFLYERWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_ZERGFLYERATTACKLEVEL1 } },
                { Upgrades.ZERGFLYERWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_ZERGFLYERATTACKLEVEL2 } },
                { Upgrades.ZERGFLYERWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE, UnitTypes.ZERG_GREATERSPIRE }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_ZERGFLYERATTACKLEVEL3 } },

                { Upgrades.LURKERSPEED, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LURKERDENMP }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ADAPTIVETALONS } },
                { Upgrades.LURKERRANGE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LURKERDENMP }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_LURKERRANGE } },

                { Upgrades.CHITINOUSPLATING, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_ULTRALISKCAVERN }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_CHITINOUSPLATING } },
                { Upgrades.ANABOLICSYNTHESIS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_ULTRALISKCAVERN }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ANABOLICSYNTHESIS } }
            };
        }
    }
}
