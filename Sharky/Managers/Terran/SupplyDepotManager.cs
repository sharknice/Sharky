using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers.Protoss
{
    public class SupplyDepotManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;

        public SupplyDepotManager(ActiveUnitData activeUnitData)
        {
            ActiveUnitData = activeUnitData;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            var actions = new List<SC2APIProtocol.Action>();

            // TODO: need to improve the supply depot lower/raise logic

            foreach (var raisedDepot in ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT))
            {
                if (!raisedDepot.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying) || WinningGround(raisedDepot))
                {
                    var action = raisedDepot.Order(frame, Abilities.MORPH_SUPPLYDEPOT_LOWER);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            foreach (var loweredDepot in ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED))
            {
                if (loweredDepot.UnitCalculation.NearbyEnemies.Any(enemy => !enemy.Unit.IsFlying) && LosingGround(loweredDepot))
                {
                    var action = loweredDepot.Order(frame, Abilities.MORPH_SUPPLYDEPOT_RAISE);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
        }

        bool WinningGround(UnitCommander unitCommander)
        {
            if (unitCommander.UnitCalculation.NearbyAllies.All(a => a.TargetPriorityCalculation.GroundWinnability > 1))
            {
                return true;
            }
            return false;
        }

        bool LosingGround(UnitCommander unitCommander)
        {
            if (!unitCommander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) || unitCommander.UnitCalculation.NearbyAllies.Any(a => a.TargetPriorityCalculation.GroundWinnability < 1))
            {
                return true;
            }
            return false;
        }
    }
}
