using System.Collections.Generic;

namespace Sharky.TypeData
{
    public class TrainingDataService
    {
        public Dictionary<UnitTypes, TrainingTypeData> TrainingData()
        {
            return new Dictionary<UnitTypes, TrainingTypeData>
            {
                { UnitTypes.TERRAN_BARRACKSTECHLAB, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Gas = 25, Ability = Abilities.BUILD_TECHLAB_BARRACKS, IsAddOn = true } },
                { UnitTypes.TERRAN_BARRACKSREACTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Gas = 50, Ability = Abilities.BUILD_REACTOR_BARRACKS, IsAddOn = true } },

                { UnitTypes.PROTOSS_PROBE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_NEXUS }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_PROBE } },
                { UnitTypes.PROTOSS_MOTHERSHIP, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_NEXUS }, Minerals = 400, Gas = 400, Food = 8, Ability = Abilities.TRAIN_MOTHERSHIP } },
                { UnitTypes.PROTOSS_ZEALOT, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 100, Food = 2, Ability = Abilities.TRAIN_ZEALOT, WarpInAbility = Abilities.TRAINWARP_ZEALOT } },
                { UnitTypes.PROTOSS_SENTRY, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 50, Gas = 100, Food = 2, Ability = Abilities.TRAIN_SENTRY, WarpInAbility = Abilities.TRAINWARP_SENTRY } },
                { UnitTypes.PROTOSS_STALKER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 125, Gas = 50, Food = 2, Ability = Abilities.TRAIN_STALKER, WarpInAbility = Abilities.TRAINWARP_STALKER } },
                { UnitTypes.PROTOSS_ADEPT, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 100, Gas = 25, Food = 2, Ability = Abilities.TRAIN_ADEPT, WarpInAbility = Abilities.TRAINWARP_ADEPT } },
                { UnitTypes.PROTOSS_HIGHTEMPLAR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 50, Gas = 150, Food = 2, Ability = Abilities.TRAIN_HIGHTEMPLAR, WarpInAbility = Abilities.TRAINWARP_HIGHTEMPLAR } },
                { UnitTypes.PROTOSS_DARKTEMPLAR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_GATEWAY, UnitTypes.PROTOSS_WARPGATE }, Minerals = 125, Gas = 125, Food = 2, Ability = Abilities.TRAIN_DARKTEMPLAR, WarpInAbility = Abilities.TRAINWARP_DARKTEMPLAR } },
                { UnitTypes.PROTOSS_OBSERVER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 25, Gas = 75, Food = 1, Ability = Abilities.TRAIN_OBSERVER } },
                { UnitTypes.PROTOSS_WARPPRISM, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 200, Food = 2, Ability = Abilities.TRAIN_WARPPRISM } },
                { UnitTypes.PROTOSS_IMMORTAL, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 275, Gas = 100, Food = 4, Ability = Abilities.TRAIN_IMMORTAL } },
                { UnitTypes.PROTOSS_COLOSSUS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 300, Gas = 200, Food = 6, Ability = Abilities.TRAIN_COLOSSUS } },
                { UnitTypes.PROTOSS_DISRUPTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY }, Minerals = 150, Gas = 150, Food = 3, Ability = Abilities.TRAIN_DISRUPTOR } },
                { UnitTypes.PROTOSS_PHOENIX, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 150, Gas = 100, Food = 2, Ability = Abilities.TRAIN_PHOENIX } },
                { UnitTypes.PROTOSS_ORACLE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 150, Gas = 150, Food = 3, Ability = Abilities.TRAIN_ORACLE } },
                { UnitTypes.PROTOSS_VOIDRAY, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 200, Gas = 150, Food = 4, Ability = Abilities.TRAIN_VOIDRAY } },
                { UnitTypes.PROTOSS_TEMPEST, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 250, Gas = 175, Food = 5, Ability = Abilities.TRAIN_TEMPEST } },
                { UnitTypes.PROTOSS_CARRIER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_STARGATE }, Minerals = 350, Gas = 250, Food = 6, Ability = Abilities.TRAIN_CARRIER } },
                { UnitTypes.PROTOSS_ARCHON, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.PROTOSS_HIGHTEMPLAR, UnitTypes.PROTOSS_DARKTEMPLAR }, Minerals = 0, Gas = 0, Food = 0, Ability = Abilities.MORPH_ARCHON } },

                { UnitTypes.TERRAN_SCV, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_COMMANDCENTER, UnitTypes.TERRAN_ORBITALCOMMAND, UnitTypes.TERRAN_PLANETARYFORTRESS }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_SCV } },
                { UnitTypes.TERRAN_MARINE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_MARINE } },
                { UnitTypes.TERRAN_MARAUDER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 100, Gas = 25, Food = 2, Ability = Abilities.TRAIN_MARAUDER, RequiresTechLab = true } },
                { UnitTypes.TERRAN_REAPER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 50, Gas = 50, Food = 1, Ability = Abilities.TRAIN_REAPER } },
                { UnitTypes.TERRAN_GHOST, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_BARRACKS }, Minerals = 150, Gas = 125, Food = 2, Ability = Abilities.TRAIN_GHOST, RequiresTechLab = true } },
                { UnitTypes.TERRAN_HELLION, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 100, Food = 2, Ability = Abilities.TRAIN_HELLION } },
                { UnitTypes.TERRAN_HELLIONTANK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 100, Food = 2, Ability = Abilities.TRAIN_HELLBAT } },
                { UnitTypes.TERRAN_CYCLONE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 150, Gas = 100, Food = 3, Ability = Abilities.TRAIN_CYCLONE, RequiresTechLab = true } },
                { UnitTypes.TERRAN_SIEGETANK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 150, Gas = 125, Food = 3, Ability = Abilities.TRAIN_SIEGETANK, RequiresTechLab = true } },
                { UnitTypes.TERRAN_WIDOWMINE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 75, Gas = 75, Food = 2, Ability = Abilities.TRAIN_WIDOWMINE } },
                { UnitTypes.TERRAN_THOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_FACTORY }, Minerals = 300, Gas = 200, Food = 6, Ability = Abilities.TRAIN_THOR, RequiresTechLab = true } },
                { UnitTypes.TERRAN_VIKINGFIGHTER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 150, Gas = 75, Food = 2, Ability = Abilities.TRAIN_VIKINGFIGHTER } },
                { UnitTypes.TERRAN_MEDIVAC, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 100, Gas = 100, Food = 2, Ability = Abilities.TRAIN_MEDIVAC } },
                { UnitTypes.TERRAN_LIBERATOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 150, Gas = 150, Food = 3, Ability = Abilities.TRAIN_LIBERATOR } },
                { UnitTypes.TERRAN_RAVEN, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 100, Gas = 200, Food = 2, Ability = Abilities.TRAIN_RAVEN, RequiresTechLab = true } },
                { UnitTypes.TERRAN_BANSHEE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 150, Gas = 100, Food = 3, Ability = Abilities.TRAIN_BANSHEE, RequiresTechLab = true } },
                { UnitTypes.TERRAN_BATTLECRUISER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_STARPORT }, Minerals = 400, Gas = 300, Food = 6, Ability = Abilities.TRAIN_BATTLECRUISER, RequiresTechLab = true } },

                { UnitTypes.ZERG_QUEEN, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HATCHERY }, Minerals = 150, Food = 2, Ability = Abilities.TRAIN_QUEEN } },
                { UnitTypes.ZERG_OVERLORD, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Ability = Abilities.TRAIN_OVERLORD } },
                { UnitTypes.ZERG_DRONE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_DRONE } },
                { UnitTypes.ZERG_ZERGLING, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 50, Food = 1, Ability = Abilities.TRAIN_ZERGLING } },          
                { UnitTypes.ZERG_ROACH, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 75, Gas = 25, Food = 2, Ability = Abilities.TRAIN_ROACH } },
                { UnitTypes.ZERG_HYDRALISK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Gas = 50, Food = 2, Ability = Abilities.TRAIN_HYDRALISK } },
                { UnitTypes.ZERG_INFESTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Gas = 150, Food = 2, Ability = Abilities.TRAIN_INFESTOR } },
                { UnitTypes.ZERG_SWARMHOSTMP, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Gas = 75, Food = 3, Ability = Abilities.TRAIN_SWARMHOST } },
                { UnitTypes.ZERG_MUTALISK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Gas = 100, Food = 2, Ability = Abilities.TRAIN_MUTALISK } },
                { UnitTypes.ZERG_CORRUPTOR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 150, Gas = 100, Food = 2, Ability = Abilities.TRAIN_CORRUPTOR } },
                { UnitTypes.ZERG_VIPER, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 100, Gas = 75, Food = 3, Ability = Abilities.TRAIN_VIPER } },
                { UnitTypes.ZERG_ULTRALISK, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LARVA }, Minerals = 300, Gas = 200, Food = 6, Ability = Abilities.TRAIN_ULTRALISK } },
            };
        }
    }
}
