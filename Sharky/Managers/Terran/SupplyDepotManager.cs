using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers.Terran
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

            foreach (var raisedDepot in ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT && c.UnitCalculation.Unit.BuildProgress == 1))
            {
                if (!raisedDepot.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying && e.FrameLastSeen == frame) || WinningGround(raisedDepot))
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
                if (loweredDepot.UnitCalculation.NearbyEnemies.Any(enemy => !enemy.Unit.IsFlying && enemy.FrameLastSeen == frame) && LosingGround(loweredDepot))
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
            if (unitCommander.UnitCalculation.NearbyAllies.Take(25).All(a => a.TargetPriorityCalculation.GroundWinnability > 1) && unitCommander.UnitCalculation.NearbyAllies.Take(25).Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit) && a.NearbyEnemies.Count() == unitCommander.UnitCalculation.NearbyEnemies.Count()))
            {
                return true;
            }
            return false;
        }

        bool LosingGround(UnitCommander unitCommander)
        {
            if (!unitCommander.UnitCalculation.NearbyAllies.Take(25).Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) || unitCommander.UnitCalculation.NearbyAllies.Take(25).Any(a => a.TargetPriorityCalculation.GroundWinnability < 1))
            {
                return true;
            }
            return false;
        }
    }
}
