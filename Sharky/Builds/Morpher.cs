using Sharky.Managers;
using System.Linq;

namespace Sharky.Builds
{
    public class Morpher
    {
        ActiveUnitData ActiveUnitData;
        UnitDataManager UnitDataManager;
        SharkyOptions SharkyOptions;

        public Morpher(ActiveUnitData activeUnitData, UnitDataManager unitDataManager, SharkyOptions sharkyOptions)
        {
            ActiveUnitData = activeUnitData;
            UnitDataManager = unitDataManager;
            SharkyOptions = sharkyOptions;
        }

        public SC2APIProtocol.Action MorphBuilding(MacroData macroData, TrainingTypeData unitData)
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
