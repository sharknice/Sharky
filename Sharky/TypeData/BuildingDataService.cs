using System.Collections.Generic;

namespace Sharky.TypeData
{
    public class BuildingDataService
    {
        public Dictionary<UnitTypes, BuildingTypeData> BuildingData()
        {
            return new Dictionary<UnitTypes, BuildingTypeData>
            {
                { UnitTypes.TERRAN_COMMANDCENTER, new BuildingTypeData { Ability = Abilities.BUILD_COMMANDCENTER, Size = 2, Minerals = 400 } },
                { UnitTypes.TERRAN_SUPPLYDEPOT, new BuildingTypeData { Ability = Abilities.BUILD_SUPPLYDEPOT, Size = 2, Minerals = 100 } },
                { UnitTypes.TERRAN_REFINERY, new BuildingTypeData { Ability = Abilities.BUILD_REFINERY, Size = 3, Minerals = 75 } },
                { UnitTypes.TERRAN_BARRACKS, new BuildingTypeData { Ability = Abilities.BUILD_BARRACKS, Size = 3, Minerals = 150 } },
                { UnitTypes.TERRAN_ENGINEERINGBAY, new BuildingTypeData { Ability = Abilities.BUILD_ENGINEERINGBAY, Size = 3, Minerals = 125 } },
                { UnitTypes.TERRAN_MISSILETURRET, new BuildingTypeData { Ability = Abilities.BUILD_MISSILETURRET, Size = 2, Minerals = 100 } },
                { UnitTypes.TERRAN_BUNKER, new BuildingTypeData { Ability = Abilities.BUILD_BUNKER, Size = 3, Minerals = 100 } },
                { UnitTypes.TERRAN_SENSORTOWER, new BuildingTypeData { Ability = Abilities.BUILD_SENSORTOWER, Size = 2, Minerals = 125, Gas = 100 } },
                { UnitTypes.TERRAN_FACTORY, new BuildingTypeData { Ability = Abilities.BUILD_FACTORY, Size = 3, Minerals = 150, Gas = 100 } },
                { UnitTypes.TERRAN_STARPORT, new BuildingTypeData { Ability = Abilities.BUILD_STARPORT, Size = 2, Minerals = 150, Gas = 100 } },
                { UnitTypes.TERRAN_ARMORY, new BuildingTypeData { Ability = Abilities.BUILD_ARMORY, Size = 3, Minerals = 150, Gas = 100 } },
                { UnitTypes.TERRAN_FUSIONCORE, new BuildingTypeData { Ability = Abilities.BUILD_FUSIONCORE, Size = 3, Minerals = 150, Gas = 150 } },
                { UnitTypes.TERRAN_GHOSTACADEMY, new BuildingTypeData { Ability = Abilities.BUILD_GHOSTACADEMY, Size = 3, Minerals = 150, Gas = 50 } },

                { UnitTypes.PROTOSS_NEXUS, new BuildingTypeData { Ability = Abilities.BUILD_NEXUS, Size = 5, Minerals = 400 } },
                { UnitTypes.PROTOSS_PYLON, new BuildingTypeData { Ability = Abilities.BUILD_PYLON, Size = 2, Minerals = 100 } },
                { UnitTypes.PROTOSS_ASSIMILATOR, new BuildingTypeData { Ability = Abilities.BUILD_ASSIMILATOR, Size = 3, Minerals = 75 } },
                { UnitTypes.PROTOSS_GATEWAY, new BuildingTypeData { Ability = Abilities.BUILD_GATEWAY, Size = 3, Minerals = 150 } },
                { UnitTypes.PROTOSS_FORGE, new BuildingTypeData { Ability = Abilities.BUILD_FORGE, Size = 3, Minerals = 150 } },
                { UnitTypes.PROTOSS_FLEETBEACON, new BuildingTypeData { Ability = Abilities.BUILD_FLEETBEACON, Size = 3, Minerals = 300, Gas = 200 } },
                { UnitTypes.PROTOSS_TWILIGHTCOUNCIL, new BuildingTypeData { Ability = Abilities.BUILD_TWILIGHTCOUNCIL, Size = 3, Minerals = 150, Gas = 100 } },
                { UnitTypes.PROTOSS_PHOTONCANNON, new BuildingTypeData { Ability = Abilities.BUILD_PHOTONCANNON, Size = 2, Minerals = 150 } },
                { UnitTypes.PROTOSS_STARGATE, new BuildingTypeData { Ability = Abilities.BUILD_STARGATE, Size = 3, Minerals = 150, Gas = 150 } },
                { UnitTypes.PROTOSS_TEMPLARARCHIVE, new BuildingTypeData { Ability = Abilities.BUILD_TEMPLARARCHIVE, Size = 3, Minerals = 150, Gas = 200 } },
                { UnitTypes.PROTOSS_DARKSHRINE, new BuildingTypeData { Ability = Abilities.BUILD_DARKSHRINE, Size = 3, Minerals = 150, Gas = 150 } },
                { UnitTypes.PROTOSS_ROBOTICSBAY, new BuildingTypeData { Ability = Abilities.BUILD_ROBOTICSBAY, Size = 3, Minerals = 200, Gas = 200 } },
                { UnitTypes.PROTOSS_ROBOTICSFACILITY, new BuildingTypeData { Ability = Abilities.BUILD_ROBOTICSFACILITY, Size = 3, Minerals = 150, Gas = 100 } },
                { UnitTypes.PROTOSS_CYBERNETICSCORE, new BuildingTypeData { Ability = Abilities.BUILD_CYBERNETICSCORE, Size = 3, Minerals = 150, } },
                { UnitTypes.PROTOSS_SHIELDBATTERY, new BuildingTypeData { Ability = Abilities.BUILD_SHIELDBATTERY, Size = 2, Minerals = 100 } }
            };
        }
    }
}
