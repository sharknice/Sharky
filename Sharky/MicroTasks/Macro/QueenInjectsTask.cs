using Sharky.Managers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class QueenInjectsTask : MicroTask
    {
        IUnitManager UnitManager;

        public QueenInjectsTask(IUnitManager unitManager, float priority)
        {
            UnitManager = unitManager;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();

            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            var hatcheryCount = UnitManager.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY);

            if (UnitCommanders.Count() >= hatcheryCount)
            {
                while (UnitCommanders.Count() > hatcheryCount)
                {
                    UnitCommanders.Remove(UnitCommanders.Last());
                }
                return;
            }

            foreach (var commander in commanders)
            {
                if (UnitCommanders.Count() < hatcheryCount)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            commands.AddRange(InjectLarva(frame));

            return commands;
        }

        List<SC2APIProtocol.Action> InjectLarva(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var hatcheries = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && !u.Value.Unit.BuffIds.Contains((uint)Buffs.QUEENSPAWNLARVATIMER));

            foreach (var hatchery in hatcheries)
            {
                var closestQueen = UnitCommanders.Where(u => u.UnitCalculation.Unit.Energy >= 25).OrderBy(u => Vector2.DistanceSquared(new Vector2(u.UnitCalculation.Unit.Pos.X, u.UnitCalculation.Unit.Pos.Y), new Vector2(hatchery.Value.Unit.Pos.X, hatchery.Value.Unit.Pos.Y))).FirstOrDefault();
                if (closestQueen != null)
                {
                    var action = closestQueen.Order(frame, Abilities.EFFECT_INJECTLARVA, null, hatchery.Key);
                    if (action != null)
                    {
                        actions.Add(action);
                        return actions;
                    }
                }
            }

            return actions;
        }
    }
}
