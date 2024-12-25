using Sharky.Extensions;

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
        EnemyData EnemyData;

        BuildingService BuildingService;
        TargetingService TargetingService;

        public bool LongDistanceMiningEnabled { get; set; }
        public bool AttackWithIdleWorkers { get; set; } = true;
        public bool RecallDistantWorkers { get; set; } = true;

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
            BuildingService = defaultSharkyBot.BuildingService;
            TargetingService = defaultSharkyBot.TargetingService;
            EnemyData = defaultSharkyBot.EnemyData;

            MineralMiner = mineralMiner;
            GasMiner = gasMiner;

            LowMineralsHighGas = false;
            LongDistanceMiningEnabled = true;

            UnitCommanders = new List<UnitCommander>();
            Enabled = true;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(c => !c.Value.Claimed))
            {
                if (commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && !UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == commander.Key))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (!BaseData.SelfBases.Any()) { return null; }

            if (frame == 0)
            {
                return SplitWorkers(frame);
            }

            var commands = new List<SC2APIProtocol.Action>();

            ReclaimBuilders(frame);
            RemoveLostWorkers(frame);
            StopAttackingWithSafeWorkers(frame);

            commands.AddRange(BalanceGasWorkers(frame));
            commands.AddRange(MineWithIdleWorkers(frame));
            commands.AddRange(TransferWorkers(frame));
            commands.AddRange(MiningDefenseService.DealWithEnemies(frame, UnitCommanders));
            commands.AddRange(MineralMiner.MineMinerals(frame));
            commands.AddRange(GasMiner.MineGas(frame));
            commands.AddRange(RecallWorkers(frame));

            var hurtWorkers = UnitCommanders.Where(c => c.UnitCalculation.Unit.Health < c.UnitCalculation.Unit.HealthMax);
            if (EnemyData.SelfRace == Race.Terran && hurtWorkers.Any())
            {
                commands.AddRange(RepairWithExtraIdleWorkers(frame, hurtWorkers));
            }
            else if (LongDistanceMiningEnabled)
            {
                commands.AddRange(DistanceMineWithExtraIdleWorkers(frame));
            }
            else
            {
                commands.AddRange(OverSaturatedWithExtraIdleWorkers(frame));
            }

            if (MacroData.Minerals < 300 && UnitCommanders.Any() && !BaseData.SelfBases.Any() && !ActiveUnitData.SelfUnits.Values.Any(u => u.UnitClassifications.HasFlag(UnitClassification.ResourceCenter)))
            {
                AttackWithWorkers();
            }

            return commands;
        }

        private void AttackWithWorkers()
        {
            foreach(var commander in UnitCommanders)
            {
                AttackWithWorker(commander);
            }
            UnitCommanders.Clear();
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

            UnitCommanders.RemoveAll(c => c.UnitRole == UnitRole.Wall);
        }

        IEnumerable<UnitCommander> GetIdleWorkers()
        {
            return UnitCommanders.Where(c => c.UnitRole == UnitRole.None && !c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_HATCHERY));
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
                        while (info.Workers.Count() > gasSaturationCount && info.Workers.Any())
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
                                var workers = unitCalculation.NearbyAllies.Where(c => c.UnitClassifications.HasFlag(UnitClassification.Worker) && !c.Unit.BuffIds.Any(b => SharkyUnitData.CarryingMineralBuffs.Contains((Buffs)b)));
                                idleWorkers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Gas && workers.Any(w => w.Unit.Tag == c.UnitCalculation.Unit.Tag)).OrderBy(c => Vector2.DistanceSquared(vector, c.UnitCalculation.Position));
                            }
                        }
                        if (idleWorkers.Any())
                        {
                            var worker = idleWorkers.FirstOrDefault();
                            worker.UnitRole = UnitRole.Minerals;
                            info.Workers.Add(worker);

                            return actions;
                        }
                    }
                }
            }
            else if (unsaturatedRefineries.Any() && saturatedRefineries.Count() < BuildOptions.MaxActiveGasCount)
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
                                    var workers = unitCalculation.NearbyAllies.Where(c => c.UnitClassifications.HasFlag(UnitClassification.Worker) && !c.Unit.BuffIds.Any(b => SharkyUnitData.CarryingMineralBuffs.Contains((Buffs)b)));
                                    idleWorkers = UnitCommanders.Where(c => c.UnitRole == UnitRole.Minerals && workers.Any(w => w.Unit.Tag == c.UnitCalculation.Unit.Tag)).OrderBy(c => Vector2.DistanceSquared(gasVector, c.UnitCalculation.Position));
                                }
                            }
                            if (idleWorkers.Any())
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
                var unsaturated = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress > .99 && b.MineralMiningInfo.Any(m => m.Workers.Count() < saturationCount)).OrderBy(u => Vector2.DistanceSquared(u.MiddleMineralLocation.ToVector2(), worker.UnitCalculation.Position));

                foreach (var selfBase in unsaturated)
                {
                    foreach (var info in selfBase.MineralMiningInfo.Where(m => m.Workers.Count() < saturationCount).OrderBy(m => m.Workers.Count()).ThenBy(m => Vector2.DistanceSquared(m.HarvestPoint.ToVector2(), worker.UnitCalculation.Position)))
                    {
                        worker.UnitRole = UnitRole.Minerals;
                        info.Workers.Add(worker);
                        MineWithIdleWorkers(frame);
                        if (ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag))
                        {
                            ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].RallyPointSet = false;
                        }
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

            if (unsaturated.Any() && overSaturated.Any())
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

            foreach (var worker in UnitCommanders.OrderByDescending(w => Vector2.DistanceSquared(w.UnitCalculation.Position, BaseData.MainBase.BehindMineralLineLocation.ToVector2())))
            {
                worker.UnitRole = UnitRole.Minerals;
                miningAssignments.Where(m => m.Workers.Count < Math.Ceiling(workersPerField)).OrderBy(m => Vector2.DistanceSquared(m.ResourceUnit.Pos.ToVector2(), worker.UnitCalculation.Position)).ThenBy(m => Vector2.Distance(m.ResourceUnit.Pos.ToVector2(), BaseData.MainBase.Location.ToVector2())).First().Workers.Add(worker);
            }

            while (miningAssignments.Any(m => m.Workers.Count() == 0))
            {
                var empty = miningAssignments.FirstOrDefault(m => m.Workers.Count() == 0);
                if (empty != null)
                {
                    var neighbor = miningAssignments.Where(m => m.Workers.Count() == 2).OrderBy(m => Vector2.Distance(m.ResourceUnit.Pos.ToVector2(), empty.ResourceUnit.Pos.ToVector2())).FirstOrDefault();
                    if (neighbor != null)
                    {
                        var worker = neighbor.Workers.OrderBy(w => Vector2.Distance(w.UnitCalculation.Position, neighbor.ResourceUnit.Pos.ToVector2())).FirstOrDefault();
                        neighbor.Workers.Remove(worker);
                        empty.Workers.Add(worker);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return miningAssignments;
        }

        List<SC2APIProtocol.Action> RecallWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            if (BaseData.SelfBases.Count() > 1 || !RecallDistantWorkers) { return actions; }
            foreach (var selfBase in BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && b.ResourceCenter.Energy >= 50))
            {
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                var workers = selfBase.MineralMiningInfo.SelectMany(m => m.Workers.Where(w => w.UnitRole == UnitRole.Minerals && Vector2.DistanceSquared(baseVector, w.UnitCalculation.Position) > 2500));
                if (workers.Any())
                {
                    var centerPoint = TargetingService.GetArmyPoint(workers);
                    var vector = new Vector2(centerPoint.X, centerPoint.Y);
                    var probes = workers.Where(c => !c.UnitCalculation.NearbyAllies.Any(a => !workers.Select(w => w.UnitCalculation).Contains(a) && Vector2.DistanceSquared(a.Position, c.UnitCalculation.Position) <= (2.5f * a.Unit.Radius) * (2.5f * a.Unit.Radius)));
                    //var probes = workers.Where(c => !c.UnitCalculation.NearbyAllies.Any(a => !workers.Any(w => w.UnitCalculation.Unit.Tag == a.Unit.Tag && Vector2.DistanceSquared(a.Position, c.UnitCalculation.Position) <= (2.5f * a.Unit.Radius) * (2.5f * a.Unit.Radius))));

                    var closestProbe = probes.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).FirstOrDefault();
                    if (closestProbe != null)
                    {
                        if (ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag))
                        {
                            var action = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].Order(frame, Abilities.NEXUSMASSRECALL, new Point2D { X = closestProbe.UnitCalculation.Position.X, Y = closestProbe.UnitCalculation.Position.Y });
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }

                }                             
            }

            return actions;
        }

        private IEnumerable<SC2APIProtocol.Action> RepairWithExtraIdleWorkers(int frame, IEnumerable<UnitCommander> hurtWorkers)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var worker in GetIdleWorkers())
            {
                if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                {
                    var action = worker.Order(frame, Abilities.HARVEST_RETURN, null, 0);
                    if (action != null) { actions.AddRange(action); }
                    continue;
                }
                else if (worker.UnitCalculation.Unit.Orders.Count() == 0 || worker.UnitCalculation.Unit.Orders.Any(o => BaseData.SelfBases.Any(b => b.MineralMiningInfo.Any(m => m.ResourceUnit.Tag == o.TargetUnitTag))))
                {
                    if (!worker.AutoCastToggled)
                    {
                        var action = worker.ToggleAutoCast(Abilities.EFFECT_REPAIR_SCV);
                        worker.AutoCastToggled = true;
                        if (action != null) { actions.AddRange(action); }
                        continue;
                    }
                    var hurtWorker = hurtWorkers.OrderBy(w => Vector2.DistanceSquared(w.UnitCalculation.Position, worker.UnitCalculation.Position)).FirstOrDefault();
                    if (hurtWorker != null)
                    {
                        var action = worker.Order(frame, Abilities.EFFECT_REPAIR, targetTag: hurtWorker.UnitCalculation.Unit.Tag);
                        if (action != null) { actions.AddRange(action); }
                        continue;
                    }
                }
            }
            

            return actions;
        }

        private IEnumerable<SC2APIProtocol.Action> OverSaturatedWithExtraIdleWorkers(int frame)
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
                else if (worker.UnitCalculation.Unit.Orders.Count() == 0 || worker.UnitCalculation.Unit.Orders.Any(o => BaseData.SelfBases.Any(b => b.MineralMiningInfo.Any(m => m.ResourceUnit.Tag == o.TargetUnitTag))))
                {
                    var closestPatch = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralFields.Any(m => m.Tag == u.Value.Unit.Tag))).OrderBy(m => Vector2.DistanceSquared(m.Value.Position, worker.UnitCalculation.Position)).FirstOrDefault().Value;
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

        private IEnumerable<SC2APIProtocol.Action> DistanceMineWithExtraIdleWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var outOfPlaceBase = GetOutOfPlaceBase();
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
                else if (AttackWithIdleWorkers && worker.UnitCalculation.NearbyEnemies.Any(e => e.FrameFirstSeen == frame && !e.Unit.IsFlying) && worker.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
                {
                    var attackTask = MicroTaskData[typeof(AttackTask).Name];
                    if (attackTask.Enabled)
                    {
                        if (!attackTask.UnitCommanders.Contains(worker))
                        {
                            worker.UnitRole = UnitRole.Attack;
                            attackTask.UnitCommanders.Add(worker);
                            if (attackTask.GetType() == typeof(AdvancedAttackTask))
                            {
                                ((AdvancedAttackTask)attackTask).SupportUnits.Add(worker);
                            }
                        }
                    }
                }
                else if (AttackWithIdleWorkers && worker.UnitCalculation.EnemiesThreateningDamage.Any(e => e.FrameFirstSeen == frame))
                {
                    var attackTask = MicroTaskData[typeof(AttackTask).Name];
                    if (attackTask.Enabled)
                    {
                        if (!attackTask.UnitCommanders.Contains(worker))
                        {
                            worker.UnitRole = UnitRole.Attack;
                            attackTask.UnitCommanders.Add(worker);
                            if (attackTask.GetType() == typeof(AdvancedAttackTask))
                            {
                                ((AdvancedAttackTask)attackTask).SupportUnits.Add(worker);
                            }
                        }
                    }
                }
                else if (worker.UnitCalculation.Unit.Orders.Count() == 0 || worker.UnitCalculation.Unit.Orders.Any(o => BaseData.SelfBases.Any(b => b.MineralMiningInfo.Any(m => m.ResourceUnit.Tag == o.TargetUnitTag))))
                {
                    var nextBase = BuildingService.GetNextBaseLocation();
                    if (outOfPlaceBase != null)
                    {
                        var closestBase = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(b.Location.ToVector2(), outOfPlaceBase.UnitCalculation.Position)).FirstOrDefault(b => !BaseData.SelfBases.Any(sb => b.Location.X == sb.Location.X && b.Location.Y == sb.Location.Y));
                        if (closestBase != null)
                        {
                            nextBase = closestBase;
                        }
                    }

                    var progressPatch = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress < 1 && b.MineralFields.Any(m => m.Tag == u.Value.Unit.Tag))).OrderBy(m => Vector2.DistanceSquared(m.Value.Position, worker.UnitCalculation.Position)).FirstOrDefault().Value;
                    if (progressPatch != null)
                    {
                        var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, progressPatch.Unit.Tag);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                    else if (nextBase?.MineralFields?.FirstOrDefault() != null)
                    {
                        var field = ActiveUnitData.NeutralUnits.Values.FirstOrDefault(f => f.Unit.Pos.X == nextBase.MineralFields.FirstOrDefault().Pos.X && f.Unit.Pos.Y == nextBase.MineralFields.FirstOrDefault().Pos.Y);
                        if (field != null)
                        {
                            var action = worker.Order(frame, Abilities.SMART, null, field.Unit.Tag);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }
                    else
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
            }
            return actions;
        }

        UnitCommander GetOutOfPlaceBase()
        {
            return ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.ResourceCenter) && c.UnitCalculation.Unit.BuildProgress > .75f && !BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.Tag == c.UnitCalculation.Unit.Tag));
        }

        private void AttackWithWorker(UnitCommander worker)
        {
            if (MicroTaskData.ContainsKey(typeof(AttackTask).Name))
            {
                var attackTask = MicroTaskData[typeof(AttackTask).Name];
                if (attackTask.Enabled)
                {
                    if (!attackTask.UnitCommanders.Contains(worker))
                    {
                        attackTask.UnitCommanders.Add(worker);
                        worker.UnitRole = UnitRole.Attack;
                        if (attackTask.GetType() == typeof(AdvancedAttackTask))
                        {
                            ((AdvancedAttackTask)attackTask).SupportUnits.Add(worker);
                        }
                    }
                }
            }
        }

        private void StopAttackingWithSafeWorkers(int frame)
        {
            if (MicroTaskData.ContainsKey(typeof(AttackTask).Name))
            {
                var attackTask = MicroTaskData[typeof(AttackTask).Name];
                if (attackTask.Enabled)
                {
                    var canWork = MacroData.Minerals < 400 && UnitCommanders.Any() && BaseData.SelfBases.Any() && ActiveUnitData.SelfUnits.Values.Any(u => u.UnitClassifications.HasFlag(UnitClassification.ResourceCenter));

                    if (!canWork) { return; }

                    var tags = new List<ulong>();
                    foreach (var worker in attackTask.UnitCommanders.Where(u => u.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && u.UnitRole != UnitRole.Proxy && (u.UnitCalculation.NearbyEnemies.Count(e => e.FrameFirstSeen == frame) == 0 || !u.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure))) && u.UnitRole == UnitRole.Attack))
                    {
                        worker.UnitRole = UnitRole.None;
                        worker.Claimed = false;
                        tags.Add(worker.UnitCalculation.Unit.Tag);
                    }
                    foreach (var tag in tags)
                    {
                        attackTask.StealUnit(attackTask.UnitCommanders.FirstOrDefault(u => u.UnitCalculation.Unit.Tag == tag));
                    }
                }
            }
        }

        public override void StealUnit(UnitCommander commander)
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                foreach (var info in selfBase.MineralMiningInfo)
                {
                    info.Workers.Remove(commander);
                }
                foreach (var info in selfBase.GasMiningInfo)
                {
                    info.Workers.Remove(commander);
                }
            }
            UnitCommanders.Remove(commander);
        }

        public override void PrintReport(int frame)
        {
            base.PrintReport(frame);
            foreach(var commander in UnitCommanders)
            {
                Console.WriteLine(commander.UnitRole);
            }
        }
    }
}
