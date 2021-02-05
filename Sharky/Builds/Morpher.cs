using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds
{
    public class Morpher
    {
        ActiveUnitData ActiveUnitData;

        public Morpher(ActiveUnitData activeUnitData)
        {
            ActiveUnitData = activeUnitData;
        }

        public List<SC2APIProtocol.Action> MorphBuilding(MacroData macroData, TrainingTypeData unitData)
        {
            if ((unitData.Food == 0 || unitData.Food <= macroData.FoodLeft) && unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1);
                if (building.Count() > 0)
                {
                    return building.First().Value.Order(macroData.Frame, unitData.Ability);
                }
            }
            return null;
        }
    }
}
