using Sharky.Managers;
using System.Linq;

namespace Sharky.Builds
{
    public class Morpher
    {
        IUnitManager UnitManager;
        UnitDataManager UnitDataManager;
        SharkyOptions SharkyOptions;

        public Morpher(IUnitManager unitManager, UnitDataManager unitDataManager, SharkyOptions sharkyOptions)
        {
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            SharkyOptions = sharkyOptions;
        }

        public SC2APIProtocol.Action MorphBuilding(MacroData macroData, TrainingTypeData unitData)
        {
            if (unitData.Food <= macroData.FoodLeft && unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(macroData.Frame, SharkyOptions.FramesPerSecond, UnitDataManager));
                if (building.Count() > 0)
                {
                    return building.First().Value.Order(macroData.Frame, unitData.Ability);
                }
            }
            return null;
        }
    }
}
