using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class MiningTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }

        UnitDataManager UnitDataManager;
        BaseManager BaseManager;
        UnitManager UnitManager;

        public MiningTask(UnitDataManager unitDataManager, BaseManager baseManager, UnitManager unitManager)
        {
            UnitDataManager = unitDataManager;
            BaseManager = baseManager;
            UnitManager = unitManager;

            UnitCommanders = new List<UnitCommander>();
        }

        public void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                {
                    if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !UnitDataManager.MiningAbilities.Contains((Abilities)o.AbilityId)))
                    {
                    }
                    else
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                }
            }
        }

        public IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (frame == 0)
            {
                return SplitWorkers(frame);
            }

            var commands = new List<SC2APIProtocol.Action>();

            commands.AddRange(MineGas(frame));
            commands.AddRange(MineWithIdleWorkers(frame));

            return commands;
        }

        IEnumerable<UnitCommander> GetIdleWorkers()
        {
            var incompleteRefineries = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress < .95f).Select(u => u.Key);
            return UnitCommanders.Where(c => c.UnitCalculation.Unit.Orders.Count() == 0 || c.UnitCalculation.Unit.Orders.Any(o => incompleteRefineries.Contains(o.TargetUnitTag)));
        }

        List<SC2APIProtocol.Action> MineGas(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var refinereries = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress >= .95f);
            var unsaturatedRefineries = refinereries.Where(u => u.Value.Unit.AssignedHarvesters < u.Value.Unit.IdealHarvesters);

            if (unsaturatedRefineries.Count() > 0)
            {
                var refinery = unsaturatedRefineries.FirstOrDefault();
                var idealGasWorkers = GetIdleWorkers();
                if (idealGasWorkers.Count() == 0)
                {
                    idealGasWorkers = UnitCommanders.Where(c => c.UnitCalculation.Unit.Orders.Any(o => UnitDataManager.GatheringAbilities.Contains((Abilities)o.AbilityId) && !refinereries.Select(r => r.Key).Contains(o.TargetUnitTag))).OrderBy(w => Vector2.DistanceSquared(new Vector2(w.UnitCalculation.Unit.Pos.X, w.UnitCalculation.Unit.Pos.Y), new Vector2(refinery.Value.Unit.Pos.X, refinery.Value.Unit.Pos.Y)));        
                }
                
                if (idealGasWorkers.Count() > 0)
                {
                    var unitCommand = idealGasWorkers.FirstOrDefault().Order(frame, Abilities.HARVEST_GATHER, null, refinery.Key);
                    if (unitCommand != null)
                    {
                        var action = new SC2APIProtocol.Action
                        {
                            ActionRaw = new ActionRaw
                            {
                                UnitCommand = unitCommand
                            }
                        };
                        actions.Add(action);
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> MineWithIdleWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var worker in GetIdleWorkers())
            {
                var mineralField = BaseManager.MainBase.MineralFields.OrderBy(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(worker.UnitCalculation.Unit.Pos.X, worker.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
                if (mineralField != null)
                {
                    var unitCommand = worker.Order(frame, Abilities.HARVEST_GATHER, null, mineralField.Tag);
                    if (unitCommand != null)
                    {
                        var action = new SC2APIProtocol.Action
                        {
                            ActionRaw = new ActionRaw
                            {
                                UnitCommand = unitCommand
                            }
                        };
                        actions.Add(action);
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> SplitWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var miningAssignment in GetSplitAssignments())
            {
                foreach (var worker in miningAssignment.Workers)
                {
                    var unitCommand = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningAssignment.ResourceUnit.Tag);
                    if (unitCommand != null)
                    {
                        var action = new SC2APIProtocol.Action
                        {
                            ActionRaw = new ActionRaw
                            {
                                UnitCommand = unitCommand
                            }
                        };
                        actions.Add(action);
                    }
                }
            }
            return actions;
        }

        List<MiningInfo> GetSplitAssignments()
        {
            var miningAssignments = new List<MiningInfo>();
            foreach (var mineralField in BaseManager.MainBase.MineralFields)
            {
                miningAssignments.Add(new MiningInfo(mineralField));
            }

            var workersPerField = UnitCommanders.Count() / (float)miningAssignments.Count();

            foreach (var worker in UnitCommanders)
            {
                miningAssignments.Where(m => m.Workers.Count < Math.Ceiling(workersPerField)).OrderBy(m => Vector2.DistanceSquared(new Vector2(m.ResourceUnit.Pos.X, m.ResourceUnit.Pos.Y), new Vector2(worker.UnitCalculation.Unit.Pos.X, worker.UnitCalculation.Unit.Pos.Y))).First().Workers.Add(worker);
            }

            return miningAssignments;
        }
    }
}
