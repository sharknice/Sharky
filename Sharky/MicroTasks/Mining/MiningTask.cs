using SC2APIProtocol;
using Sharky.Builds;
using Sharky.MicroTasks.Mining;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class MiningTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        public MiningDefenseService MiningDefenseService { get; set; }
        MacroData MacroData;
        BuildOptions BuildOptions;

        bool LowMineralsHighGas;

        public MiningTask(SharkyUnitData sharkyUnitData, BaseData baseData, ActiveUnitData activeUnitData, float priority, MiningDefenseService miningDefenseService, MacroData macroData, BuildOptions buildOptions)
        {
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            ActiveUnitData = activeUnitData;
            Priority = priority;
            MiningDefenseService = miningDefenseService;
            MacroData = macroData;
            BuildOptions = buildOptions;

            LowMineralsHighGas = false;

            UnitCommanders = new List<UnitCommander>();
            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == commander.Key))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (frame == 0)
            {
                return SplitWorkers(frame);
            }

            var commands = new List<SC2APIProtocol.Action>();

            ReclaimBuliders(frame);
            RemoveLostWorkers();

            commands.AddRange(BalanceGasWorkers(frame));
            commands.AddRange(MineWithIdleWorkers(frame));
            commands.AddRange(TransferWorkers(frame));
            commands.AddRange(MiningDefenseService.DealWithEnemies(frame, UnitCommanders));
            commands.AddRange(MineMinerals(frame));
            commands.AddRange(MineGas(frame));
            commands.AddRange(RecallWorkers(frame));

            return commands;
        }

        void ReclaimBuliders(int frame)
        {
            var incompleteRefineries = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress < .99).Select(u => u.Key);

            var workers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Build && c.LastOrderFrame < frame - 5 && (c.UnitCalculation.Unit.Orders.Count() == 0 || !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))));
            foreach (var worker in workers)
            {
                worker.UnitRole = UnitRole.None;
            }
        }

        void RemoveLostWorkers()
        {
            foreach (var commander in UnitCommanders.Where(c => c.UnitRole == UnitRole.Minerals))
            {
                if (!BaseData.SelfBases.Any(selfBase => selfBase.MineralMiningInfo.Any(i => i.Workers.Any(w => w.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.UnitRole = UnitRole.None;
                }
            }
            foreach (var commander in UnitCommanders.Where(c => c.UnitRole == UnitRole.Gas))
            {
                if (!BaseData.SelfBases.Any(selfBase => selfBase.GasMiningInfo.Any(i => i.Workers.Any(w => w.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.UnitRole = UnitRole.None;
                }
            }

            foreach (var selfBase in BaseData.SelfBases)
            {
                foreach (var info in selfBase.MineralMiningInfo)
                {
                    info.Workers.RemoveAll(w => w.UnitRole != UnitRole.Minerals || !UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == w.UnitCalculation.Unit.Tag));
                }
                foreach (var info in selfBase.GasMiningInfo)
                {
                    info.Workers.RemoveAll(w => w.UnitRole != UnitRole.Gas || !UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == w.UnitCalculation.Unit.Tag));
                }
            }
        }

        IEnumerable<UnitCommander> GetIdleWorkers()
        {
            return UnitCommanders.Where(c => c.UnitRole == UnitRole.None);
        }

        List<SC2APIProtocol.Action> MineMinerals(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var selfBase in BaseData.SelfBases)
            {
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                foreach (var miningInfo in selfBase.MineralMiningInfo)
                {
                    var mineralVector = new Vector2(miningInfo.ResourceUnit.Pos.X, miningInfo.ResourceUnit.Pos.Y);
                    foreach (var worker in miningInfo.Workers.Where(w => w.UnitRole == UnitRole.Minerals))
                    {
                        var workerVector = worker.UnitCalculation.Position;
                        if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                        {
                            var distanceSquared = Vector2.DistanceSquared(baseVector, workerVector);
                            if (distanceSquared > 25 || distanceSquared < 10)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_RETURN, null, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.DropOffPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                        else
                        {
                            var touchingWorker = worker.UnitCalculation.NearbyAllies.Any(w => Vector2.DistanceSquared(workerVector, w.Position) < .5);
                            if (touchingWorker || Vector2.DistanceSquared(mineralVector, workerVector) < 4)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningInfo.ResourceUnit.Tag, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.HarvestPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> MineGas(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var selfBase in BaseData.SelfBases)
            {
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                foreach (var miningInfo in selfBase.GasMiningInfo)
                {
                    if (miningInfo.Workers.Count() > 0 && miningInfo.ResourceUnit.VespeneContents == 0)
                    {
                        foreach (var worker in miningInfo.Workers)
                        {
                            worker.UnitRole = UnitRole.None;
                        }
                        miningInfo.Workers.Clear();
                    }

                    var mineralVector = new Vector2(miningInfo.ResourceUnit.Pos.X, miningInfo.ResourceUnit.Pos.Y);
                    foreach (var worker in miningInfo.Workers.Where(w => w.UnitRole == UnitRole.Gas))
                    {
                        if (miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.PROTOSS_ASSIMILATORRICH || miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.TERRAN_REFINERYRICH || miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.ZERG_EXTRACTORRICH)
                        {
                            if (worker.LastTargetTag != miningInfo.ResourceUnit.Tag && worker.LastAbility != Abilities.SMART)
                            {
                                var action = worker.Order(frame, Abilities.SMART, null, miningInfo.ResourceUnit.Tag, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            continue;
                        }
                        var workerVector = worker.UnitCalculation.Position;
                        if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                        {
                            var distanceSquared = Vector2.DistanceSquared(baseVector, workerVector);
                            if (distanceSquared > 25 || distanceSquared < 10)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_RETURN, null, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.DropOffPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                        else
                        {
                            var touchingWorker = worker.UnitCalculation.NearbyAllies.Any(w => Vector2.DistanceSquared(workerVector, w.Position) < .5);
                            if (touchingWorker || Vector2.DistanceSquared(mineralVector, workerVector) < 4)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningInfo.ResourceUnit.Tag, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.HarvestPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> BalanceGasWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var gasSaturationCount = 3;
            if (BuildOptions.StrictWorkersPerGas)
            {
                gasSaturationCount = BuildOptions.StrictWorkersPerGasCount;
                foreach (var selfBase in BaseData.SelfBases)
                {
                    foreach (var info in selfBase.GasMiningInfo)
                    {
                        while (info.Workers.Count() > gasSaturationCount)
                        {
                            info.Workers.RemoveAt(0);
                        }
                    }
                }
            }

            var refinereries = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress >= .99 && BaseData.SelfBases.Any(b => b.GasMiningInfo.Any(g => g.ResourceUnit.Tag == u.Value.Unit.Tag)));
            var unsaturatedRefineries = refinereries.Where(u => BaseData.SelfBases.Any(b => b.GasMiningInfo.Any(g => g.ResourceUnit.VespeneContents > 0 && g.ResourceUnit.Tag == u.Value.Unit.Tag && g.Workers.Count() < gasSaturationCount)));
            var unsaturatedMinerals = BaseData.SelfBases.Any(b => b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < 2));

            if (LowMineralsHighGas)
            {
                if (MacroData.Minerals > 1000 || MacroData.VespeneGas < 300)
                {
                    LowMineralsHighGas = false;
                }
            }
            else
            {
                if (MacroData.Minerals < 1000 && MacroData.VespeneGas > 2500)
                {
                    LowMineralsHighGas = true;
                }
                else if (MacroData.Minerals < 500 && MacroData.VespeneGas > 600)
                {
                    LowMineralsHighGas = true;
                }
            }

            if (LowMineralsHighGas && unsaturatedMinerals)
            {
                foreach (var selfBase in BaseData.SelfBases.Where(b => b.MineralMiningInfo.Any(m => m.Workers.Count() < 2)))
                {
                    foreach (var info in selfBase.GasMiningInfo)
                    {
                        if (info.Workers.Count() > 0)
                        {
                            foreach (var worker in info.Workers)
                            {
                                worker.UnitRole = UnitRole.None;
                            }
                            info.Workers.Clear();
                        }
                    }
                }
            }
            else if (unsaturatedRefineries.Count() > 0)
            {
                foreach (var selfBase in BaseData.SelfBases)
                {
                    foreach (var info in selfBase.GasMiningInfo.Where(g => g.ResourceUnit.VespeneContents > 0))
                    {
                        if (info.Workers.Count() < gasSaturationCount)
                        {
                            var idleWorkers = GetIdleWorkers();
                            if (idleWorkers.Count() == 0 && !LowMineralsHighGas)
                            {
                                var gasVector = new Vector2(info.ResourceUnit.Pos.X, info.ResourceUnit.Pos.Y);
                                if (ActiveUnitData.SelfUnits.ContainsKey(info.ResourceUnit.Tag))
                                {
                                    var unitCalculation = ActiveUnitData.SelfUnits[info.ResourceUnit.Tag];
                                    var workers = unitCalculation.NearbyAllies.Where(c => c.UnitClassifications.Contains(UnitClassification.Worker) && !c.Unit.BuffIds.Any(b => SharkyUnitData.CarryingMineralBuffs.Contains((Buffs)b)));
                                    idleWorkers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Minerals && workers.Any(w => w.Unit.Tag == c.UnitCalculation.Unit.Tag)).OrderBy(c => Vector2.DistanceSquared(gasVector, c.UnitCalculation.Position));
                                }
                            }
                            if (idleWorkers.Count() > 0)
                            {
                                var worker = idleWorkers.FirstOrDefault();
                                worker.UnitRole = UnitRole.Gas;
                                info.Workers.Add(worker);

                                return actions;
                            }
                        }
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
                var saturationCount = 2;
                var unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < saturationCount));
                if (unsaturated.Count() == 0)
                {
                    saturationCount++;
                    unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < saturationCount)).OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), worker.UnitCalculation.Position));
                }

                foreach (var selfBase in unsaturated)
                {
                    foreach (var info in selfBase.MineralMiningInfo.Where(m => m.Workers.Count() < saturationCount))
                    {
                        worker.UnitRole = UnitRole.Minerals;
                        info.Workers.Add(worker);
                        return actions;
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> TransferWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < 2));
            var overSaturated = BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() > 2));

            if (unsaturated.Count() > 0 && overSaturated.Count() > 0)
            {
                foreach (var selfBase in unsaturated)
                {
                    foreach (var info in selfBase.MineralMiningInfo.Where(m => m.Workers.Count() < 2))
                    {
                        foreach (var overBase in overSaturated)
                        {
                            foreach (var overInfo in overBase.MineralMiningInfo.Where(m => m.Workers.Count() > 2))
                            {
                                var worker = overInfo.Workers.FirstOrDefault();
                                overInfo.Workers.Remove(worker);
                                worker.UnitRole = UnitRole.Minerals;
                                info.Workers.Add(worker);
                                return actions;
                            }
                        }
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> SplitWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var assignments = GetSplitAssignments();
            BaseData.SelfBases[0].MineralMiningInfo = assignments;
            foreach (var miningAssignment in assignments)
            {
                foreach (var worker in miningAssignment.Workers)
                {
                    var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningAssignment.ResourceUnit.Tag);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
            return actions;
        }

        List<MiningInfo> GetSplitAssignments()
        {
            var miningAssignments = new List<MiningInfo>();
            foreach (var mineralField in BaseData.MainBase.MineralFields)
            {
                miningAssignments.Add(new MiningInfo(mineralField, BaseData.MainBase.ResourceCenter.Pos));
            }

            var workersPerField = UnitCommanders.Count() / (float)miningAssignments.Count();

            foreach (var worker in UnitCommanders)
            {
                worker.UnitRole = UnitRole.Minerals;
                miningAssignments.Where(m => m.Workers.Count < Math.Ceiling(workersPerField)).OrderBy(m => Vector2.DistanceSquared(new Vector2(m.ResourceUnit.Pos.X, m.ResourceUnit.Pos.Y), worker.UnitCalculation.Position)).First().Workers.Add(worker);
            }

            return miningAssignments;
        }

        List<SC2APIProtocol.Action> RecallWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            if (BaseData.SelfBases.Count() > 1) { return actions; }
            foreach (var selfBase in BaseData.SelfBases.Where(b => b.ResourceCenter.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && b.ResourceCenter.Energy >= 50))
            {
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                foreach (var miningInfo in selfBase.MineralMiningInfo)
                {
                    foreach (var worker in miningInfo.Workers.Where(w => w.UnitRole == UnitRole.Minerals))
                    {
                        if (Vector2.DistanceSquared(baseVector, worker.UnitCalculation.Position) > 2500)
                        {
                            if (ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag))
                            {
                                var action = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].Order(frame, Abilities.NEXUSMASSRECALL, new Point2D { X = worker.UnitCalculation.Position.X, Y = worker.UnitCalculation.Position.Y });
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            
                        }
                    }
                }
            }

            return actions;
        }
    }
}
