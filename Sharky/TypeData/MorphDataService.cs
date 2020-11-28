using System.Collections.Generic;

namespace Sharky.TypeData
{
    public class MorphDataService
    {
        public Dictionary<UnitTypes, TrainingTypeData> MorphData()
        {
            return new Dictionary<UnitTypes, TrainingTypeData>
            {
                { UnitTypes.TERRAN_PLANETARYFORTRESS, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_COMMANDCENTER }, Minerals = 150, Gas = 150, Ability = Abilities.MORPH_PLANETARYFORTRESS } },
                { UnitTypes.TERRAN_ORBITALCOMMAND, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_COMMANDCENTER }, Minerals = 150, Ability = Abilities.MORPH_ORBITALCOMMAND } },

                { UnitTypes.ZERG_LAIR, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_HATCHERY }, Minerals = 150, Gas = 100, Ability = Abilities.MORPH_LAIR } },
                { UnitTypes.ZERG_HIVE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_LAIR }, Minerals = 200, Gas = 150, Ability = Abilities.MORPH_HIVE } },
                { UnitTypes.ZERG_GREATERSPIRE, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.ZERG_SPIRE }, Minerals = 100, Gas = 150, Ability = Abilities.MORPH_GREATERSPIRE } },
            };
        }
    }
}
