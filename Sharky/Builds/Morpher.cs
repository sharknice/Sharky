using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds
{
    public class Morpher
    {
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;

        public Morpher(ActiveUnitData activeUnitData, TargetingData targetingData)
        {
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
        }

        public List<SC2APIProtocol.Action> MorphBuilding(MacroData macroData, TrainingTypeData unitData)
        {
            if ((unitData.Food == 0 || unitData.Food <= macroData.FoodLeft) && unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y)));
                if (building.Count() > 0)
                {
                    return building.First().Value.Order(macroData.Frame, unitData.Ability);
                }
            }
            return null;
        }
    }
}
