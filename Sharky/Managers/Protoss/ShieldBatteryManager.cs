using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers.Protoss
{
    public class ShieldBatteryManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;

        float RestoreRange;

        public ShieldBatteryManager(ActiveUnitData activeUnitData)
        {
            ActiveUnitData = activeUnitData;
            RestoreRange = 6f + 1.125f; // 6 range and 1.125 radius
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var shieldBatteries = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && c.UnitCalculation.Unit.BuildProgress == 1 && c.UnitCalculation.Unit.Energy > 0 && c.UnitCalculation.Unit.IsPowered);
            var unitsBeingRestored = shieldBatteries.SelectMany(s => s.UnitCalculation.Unit.Orders);
            foreach (var shieldBattery in shieldBatteries)
            {
                var action = Restore(shieldBattery, unitsBeingRestored, observation.Observation.GameLoop);
                if (action != null)
                {
                    actions.AddRange(action);
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> Restore(UnitCommander shieldBattery, IEnumerable<UnitOrder> unitsBeingRestored, uint frame)
        {
            var targets = shieldBattery.UnitCalculation.NearbyAllies.Where(a => a.Unit.BuildProgress == 1 && a.Unit.Shield < a.Unit.ShieldMax - 5 && (!a.Attributes.Contains(Attribute.Structure) || a.Unit.Shield < 10) && !(ActiveUnitData.Commanders.ContainsKey(a.Unit.Tag) && ActiveUnitData.Commanders[a.Unit.Tag].UnitRole == UnitRole.Die));
            var target = targets.Where(a => !unitsBeingRestored.Any(o => o.TargetUnitTag == a.Unit.Tag) && Vector2.DistanceSquared(a.Position, shieldBattery.UnitCalculation.Position) <= (RestoreRange + a.Unit.Radius) * (RestoreRange + a.Unit.Radius)).OrderByDescending(a => a.Dps).ThenBy(a => a.Unit.Shield).FirstOrDefault(); 
            if (target != null)
            {
                var order = shieldBattery.UnitCalculation.Unit.Orders.FirstOrDefault();
                if (order != null)
                {
                    var currentTarget = shieldBattery.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.Tag == order.TargetUnitTag);
                    if (currentTarget != null && currentTarget.Dps > target.Dps)
                    {
                        return null;
                    }
                }
                return shieldBattery.Order((int)frame, Abilities.SMART, null, target.Unit.Tag, true);
            }

            return null;
        }
    }
}
