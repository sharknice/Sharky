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
                { UnitTypes.TERRAN_ORBITALCOMMAND, new TrainingTypeData { ProducingUnits = new HashSet<UnitTypes> { UnitTypes.TERRAN_COMMANDCENTER }, Minerals = 150, Ability = Abilities.MORPH_ORBITALCOMMAND } }
            };
        }
    }
}
