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
                { Upgrades.WARPGATERESEARCH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 50, Gas = 50, Ability = Abilities.RESEARCH_WARPGATE } },
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL1 } },
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL2 } },
                { Upgrades.PROTOSSGROUNDWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDWEAPONSLEVEL3 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL1 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL2 } },
                { Upgrades.PROTOSSGROUNDARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PROTOSSGROUNDARMORLEVEL3 } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL1 } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 175, Gas = 175, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL2 } },
                { Upgrades.PROTOSSAIRWEAPONSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 250, Gas = 250, Ability = Abilities.RESEARCH_PROTOSSAIRWEAPONSLEVEL3 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL1 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL2 } },
                { Upgrades.PROTOSSAIRARMORSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSAIRARMORLEVEL3 } },
                { Upgrades.PROTOSSSHIELDSLEVEL1, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL1 } },
                { Upgrades.PROTOSSSHIELDSLEVEL2, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 225, Gas = 225, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL2 } },
                { Upgrades.PROTOSSSHIELDSLEVEL3, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FORGE }, Minerals = 300, Gas = 300, Ability = Abilities.RESEARCH_PROTOSSSHIELDSLEVEL3 } },
                { Upgrades.GRAVITICDRIVE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_GRAVITICDRIVE } },
                { Upgrades.EXTENDEDTHERMALLANCE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSBAY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_EXTENDEDTHERMALLANCE } },
                { Upgrades.PSISTORMTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TEMPLARARCHIVE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_PSISTORM } },
                { Upgrades.DARKTEMPLARBLINKUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_DARKSHRINE }, Minerals = 200, Gas = 200, Ability = Abilities.RESEARCH_SHADOWSTRIKE } },
                { Upgrades.BLINKTECH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_BLINK } },
                { Upgrades.CHARGE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_TWILIGHTCOUNCIL }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_CHARGE } },
                { Upgrades.TECTONICDESTABILIZERS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_TECTONICDESTABILIZERS } },
                { Upgrades.PHOENIXRANGEUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_FLEETBEACON }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PHOENIXANIONPULSECRYSTALS } }
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
                { Upgrades.NEOSTEELFRAME, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_ENGINEERINGBAY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_NEOSTEELFRAME } },

                { Upgrades.PERSONALCLOAKING, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_GHOSTACADEMY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_PERSONALCLOAKING } },
                { Upgrades.ENHANCEDMUNITIONS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_GHOSTACADEMY }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ENHANCEDMUNITIONS } },

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
                { Upgrades.LIBERATORAGRANGEUPGRADE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FUSIONCORE }, Minerals = 150, Gas = 150, Ability = Abilities.RESEARCH_ADVANCEDBALLISTICS } },
                { Upgrades.MEDIVACINCREASESPEEDBOOST, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FUSIONCORE }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_RAPIDFIRELAUNCHERS } },

                { Upgrades.SHIELDWALL, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_COMBATSHIELD } },
                { Upgrades.STIMPACK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_STIMPACK } },
                { Upgrades.HIGHCAPACITYBARRELS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKSTECHLAB }, Minerals = 50, Gas = 50, Ability = Abilities.RESEARCH_CONCUSSIVESHELLS } },

                { Upgrades.PUNISHERGRENADES, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_INFERNALPREIGNITER } },
                { Upgrades.MAGFIELDLAUNCHERS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORYTECHLAB }, Minerals = 100, Gas = 100, Ability = Abilities.RESEARCH_MAGFIELDLAUNCHERS } },
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
            };
        }
    }
}
