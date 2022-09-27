using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers.Protoss
{
    public class PhotonCannonManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;

        public PhotonCannonManager(ActiveUnitData activeUnitData)
        {
            ActiveUnitData = activeUnitData;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var cannons = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && c.UnitCalculation.Unit.BuildProgress == 1);
            foreach (var cannon in cannons)
            {
                var action = Attack(cannon, (int)observation.Observation.GameLoop);
                if (action != null)
                {
                    actions.AddRange(action);
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> Attack(UnitCommander commander, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var oneShotKills = commander.UnitCalculation.EnemiesInRange.Where(a => a.Unit.Health + a.Unit.Shield < 20);
            if (oneShotKills.Count() > 0)
            {
                if (existingAttackOrder != null)
                {
                    var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return new List<SC2APIProtocol.Action>();
                    }
                }

                commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();

                return commander.Order(frame, Abilities.ATTACK, null, commander.BestTarget.Unit.Tag);
            }

            
            var bestDpsReduction = commander.UnitCalculation.EnemiesInRange.OrderByDescending(enemy => enemy.Dps / enemy.SimulatedHitpoints).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (bestDpsReduction != null)
            {
                return commander.Order(frame, Abilities.ATTACK, null, bestDpsReduction.Unit.Tag);
            }

            var markedForDeath = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => ActiveUnitData.Commanders.ContainsKey(a.Unit.Tag) && ActiveUnitData.Commanders[a.Unit.Tag].UnitRole == UnitRole.Die);
            if (markedForDeath != null)
            {
                return commander.Order(frame, Abilities.ATTACK, targetTag: markedForDeath.Unit.Tag);
            }

            return null;
        }
    }
}
