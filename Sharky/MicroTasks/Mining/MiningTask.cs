using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
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
        MineralMiner MineralMiner;
        GasMiner GasMiner;

        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        public MiningDefenseService MiningDefenseService { get; set; }
        MacroData MacroData;
        BuildOptions BuildOptions;
        MicroTaskData MicroTaskData;

        bool LowMineralsHighGas;

        public MiningTask(DefaultSharkyBot defaultSharkyBot, float priority, MiningDefenseService miningDefenseService, MineralMiner mineralMiner, GasMiner gasMiner)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BaseData = defaultSharkyBot.BaseData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            Priority = priority;
            MiningDefenseService = miningDefenseService;
            MacroData = defaultSharkyBot.MacroData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            MicroTaskData = defaultSharkyBot.MicroTaskData;

            MineralMiner = mineralMiner;
            GasMiner = gasMiner;

            LowMineralsHighGas = false;

            UnitCommanders = new List<UnitCommander>();
            Enabled = true;
        }

        public MiningTask(SharkyUnitData sharkyUnitData, BaseData baseData, ActiveUnitData activeUnitData, float priority, MiningDefenseService miningDefenseService, MacroData macroData, BuildOptions buildOptions, MicroTaskData microTaskData, MineralMiner mineralMiner, GasMiner gasMiner)
        {
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            ActiveUnitData = activeUnitData;
            Priority = priority;
            MiningDefenseService = miningDefenseService;
            MacroData = macroData;
            BuildOptions = buildOptions;
            MicroTaskData = microTaskData;

            MineralMiner = mineralMiner;
            GasMiner = gasMiner;

            LowMineralsHighGas = false;

            UnitCommanders = new List<UnitCommander>();
            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(c => !c.Value.Claimed))
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

            ReclaimBuilders(frame);
            RemoveLostWorkers(frame);
            StopAttackingWithSafeWorkers();

            commands.AddRange(BalanceGasWorkers(frame));
            commands.AddRange(MineWithIdleWorkers(frame));
            commands.AddRange(TransferWorkers(frame));
            commands.AddRange(MiningDefenseService.DealWithEnemies(frame, UnitCommanders));
            commands.AddRange(MineralMiner.MineMinerals(frame));
            commands.AddRange(GasMiner.MineGas(frame));
            commands.AddRange(RecallWorkers(frame));

            commands.AddRange(DistanceMineWithExtraIdleWorkers(frame));

            return commands;
        }

        void ReclaimBuilders(int frame)
        {
            var incompleteRefineries = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress < .99).Select(u => u.Key);

            var workers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Build && c.LastOrderFrame < frame - 5 && (c.UnitCalculation.Unit.Orders.Count() == 0 || !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))));
            foreach (var worker in workers)
            {
                worker.UnitRole = UnitRole.None;
            }
        }

        void RemoveLostWorkers(int frame)
        {
            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitRole == UnitRole.Minerals)
                {
                    if (!BaseData.SelfBases.Any(selfBase => selfBase.MineralMiningInfo.Any(i => i.Workers.Any(w => w.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                    {
                        commander.UnitRole = UnitRole.None;
                    }
                }
                else if (commander.UnitRole == UnitRole.Gas)
                {
                    if (!BaseData.SelfBases.Any(selfBase => selfBase.GasMiningInfo.Any(i => i.Workers.Any(w => w.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                    {
                        commander.UnitRole = UnitRole.None;
                    }
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
                        while (info.Workers.Count() > gasSaturationCount && info.Workers.Count() > 0)
                        {
                            info.Workers.RemoveAt(0);
                        }
                    }
                }
            }

            var refinereries = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress >= .99 && BaseData.SelfBases.Any(b => b.GasMiningInfo.Any(g => g.ResourceUnit.Tag == u.Value.Unit.Tag)));
            var unsaturatedRefineries = refinereries.Where(u => BaseData.SelfBases.Any(b => b.GasMiningInfo.Any(g => g.ResourceUnit.VespeneContents > 0 && g.ResourceUnit.Tag == u.Value.Unit.Tag && g.Workers.Count() < gasSaturationCount)));
            var saturatedRefineries = refinereries.Where(u => BaseData.SelfBases.Any(b => b.GasMiningInfo.Any(g => g.ResourceUnit.VespeneContents > 0 && g.ResourceUnit.Tag == u.Value.Unit.Tag && g.Workers.Count() >= gasSaturationCount)));
            var unsaturatedMinerals = BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < 2));
            var saturatedMinerals = BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() >= 2));

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
                foreach (var selfBase in BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.MineralMiningInfo.Any(m => m.Workers.Count() < 2)))
                {
                    foreach (var info in selfBase.MineralMiningInfo.Where(m => m.Workers.Count() < 2))
                    {
                        var idleWorkers = GetIdleWorkers();
                        if (idleWorkers.Count() == 0)
                        {
                            var vector = new Vector2(info.ResourceUnit.Pos.X, info.ResourceUnit.Pos.Y);
                            if (ActiveUnitData.SelfUnits.ContainsKey(selfBase.ResourceCenter.Tag))
                            {
                                var unitCalculation = ActiveUnitData.SelfUnits[selfBase.ResourceCenter.Tag];
                                var workers = unitCalculation.NearbyAllies.Where(c => c.UnitClassifications.Contains(UnitClassification.Worker) && !c.Unit.BuffIds.Any(b => SharkyUnitData.CarryingMineralBuffs.Contains((Buffs)b)));
                                idleWorkers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Gas && workers.Any(w => w.Unit.Tag == c.UnitCalculation.Unit.Tag)).OrderBy(c => Vector2.DistanceSquared(vector, c.UnitCalculation.Position));
                            }
                        }
                        if (idleWorkers.Count() > 0)
                        {
                            var worker = idleWorkers.FirstOrDefault();
                            worker.UnitRole = UnitRole.Minerals;
                            info.Workers.Add(worker);

                            return actions;
                        }
                    }
                }
            }
            else if (unsaturatedRefineries.Count() > 0 && saturatedRefineries.Count() < BuildOptions.MaxActiveGasCount)
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
                var unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress > .99 && b.MineralMiningInfo.Any(m => m.Workers.Count() < saturationCount));

                foreach (var selfBase in unsaturated)
                {
                    foreach (var info in selfBase.MineralMiningInfo.Where(m => m.Workers.Count() < saturationCount).OrderBy(m => m.Workers.Count()))
                    {
                        worker.UnitRole = UnitRole.Minerals;
                        info.Workers.Add(worker);
                        MineWithIdleWorkers(frame);
                        return actions;
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> TransferWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() < 2));
            var overSaturated = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralMiningInfo.Any(m => m.Workers.Count() > 2));

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
                    var action = worker.Order(frame, Abilities.MOVE, miningAssignment.HarvestPoint);
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
                miningAssignments.Where(m => m.Workers.Count < Math.Ceiling(workersPerField)).OrderBy(m => m.Workers.Count()).ThenBy(m => Vector2.DistanceSquared(new Vector2(m.ResourceUnit.Pos.X, m.ResourceUnit.Pos.Y), worker.UnitCalculation.Position)).First().Workers.Add(worker);
            }

            return miningAssignments;
        }

        List<SC2APIProtocol.Action> RecallWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            if (BaseData.SelfBases.Count() > 1) { return actions; }
            foreach (var selfBase in BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && b.ResourceCenter.Energy >= 50))
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

        private IEnumerable<SC2APIProtocol.Action> DistanceMineWithExtraIdleWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var worker in GetIdleWorkers())
            {
                if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                {
                    var action = worker.Order(frame, Abilities.HARVEST_RETURN, null, 0);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
                else if (worker.UnitCalculation.NearbyEnemies.Any() && worker.UnitCalculation.NearbyAllies.Take(25).Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
                {
                    var attackTask = MicroTaskData.MicroTasks["AttackTask"];
                    if (attackTask.Enabled)
                    {
                        worker.UnitRole = UnitRole.Attack;
                        attackTask.UnitCommanders.Add(worker);
                    }
                }
                else if (worker.UnitCalculation.Unit.Orders.Count() == 0 || worker.UnitCalculation.Unit.Orders.Any(o => BaseData.SelfBases.Any(b => b.MineralMiningInfo.Any(m => m.ResourceUnit.Tag == o.TargetUnitTag))))
                {
                    var closestPatch = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && !BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralFields.Any(m => m.Tag == u.Value.Unit.Tag))).OrderBy(m => Vector2.DistanceSquared(m.Value.Position, worker.UnitCalculation.Position)).FirstOrDefault().Value;
                    if (closestPatch != null)
                    {
                        var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, closestPatch.Unit.Tag);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                    else
                    {
                        AttackWithWorker(worker);
                    }
                }
            }
            return actions;
        }

        private void AttackWithWorker(UnitCommander worker)
        {
            if (MicroTaskData.MicroTasks.ContainsKey("AttackTask"))
            {
                var attackTask = MicroTaskData.MicroTasks["AttackTask"];
                if (attackTask.Enabled)
                {
                    attackTask.UnitCommanders.Add(worker);
                    worker.UnitRole = UnitRole.Attack;
                }
            }
        }

        private void StopAttackingWithSafeWorkers()
        {
            if (MicroTaskData.MicroTasks.ContainsKey("AttackTask"))
            {
                var attackTask = MicroTaskData.MicroTasks["AttackTask"];
                if (attackTask.Enabled)
                {
                    var tags = new List<ulong>();
                    foreach (var worker in attackTask.UnitCommanders.Where(u => u.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && (u.UnitCalculation.NearbyEnemies.Count() == 0 || !u.UnitCalculation.NearbyAllies.Take(25).Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure))) && u.UnitRole == UnitRole.Attack))
                    {
                        worker.UnitRole = UnitRole.None;
                        worker.Claimed = false;
                        tags.Add(worker.UnitCalculation.Unit.Tag);
                    }
                    foreach (var tag in tags)
                    {
                        attackTask.UnitCommanders.RemoveAll(u => u.UnitCalculation.Unit.Tag == tag);
                    }
                }
            }
        }
    }
}
