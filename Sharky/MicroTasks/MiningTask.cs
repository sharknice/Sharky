using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class MiningTask : MicroTask
    {
        UnitDataManager UnitDataManager;
        IBaseManager BaseManager;
        IUnitManager UnitManager;

        public MiningTask(UnitDataManager unitDataManager, IBaseManager baseManager, IUnitManager unitManager, float priority)
        {
            UnitDataManager = unitDataManager;
            BaseManager = baseManager;
            UnitManager = unitManager;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();

            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
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

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (frame == 0)
            {
                return SplitWorkers(frame);
            }

            var commands = new List<SC2APIProtocol.Action>();

            commands.AddRange(ReturnResources(frame));
            commands.AddRange(MineGas(frame));
            commands.AddRange(MineWithIdleWorkers(frame));
            commands.AddRange(TransferWorkers(frame));
            commands.AddRange(DealWithEnemies(frame));

            return commands;
        }

        IEnumerable<UnitCommander> GetIdleWorkers()
        {
            var incompleteRefineries = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress < .95f).Select(u => u.Key);
            return UnitCommanders.Where(c => c.UnitCalculation.Unit.Orders.Count() == 0 || c.UnitCalculation.Unit.Orders.Any(o => incompleteRefineries.Contains(o.TargetUnitTag)));
        }

        List<SC2APIProtocol.Action> ReturnResources(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var worker in UnitCommanders.Where(u => u.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM) && u.UnitCalculation.Unit.BuffIds.Any(b => UnitDataManager.CarryingMineralBuffs.Contains((Buffs)b))))
            {
                if (worker.UnitCalculation.NearbyAllies.Any(a => (a.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM) && Vector2.DistanceSquared(new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y), new Vector2(worker.UnitCalculation.Unit.Pos.X, worker.UnitCalculation.Unit.Pos.Y)) < .1))
                {
                    var action = worker.Order(frame, Abilities.HARVEST_RETURN);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }
            }

            return actions;
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
                    var action = idealGasWorkers.FirstOrDefault().Order(frame, Abilities.HARVEST_GATHER, null, refinery.Key);
                    if (action != null)
                    {
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
                var baseLocation = BaseManager.SelfBases.Where(b => b.ResourceCenter.BuildProgress > .9).OrderBy(b => b.ResourceCenter.AssignedHarvesters - b.ResourceCenter.IdealHarvesters).FirstOrDefault();
                if (baseLocation != null)
                {
                    var mineralField = baseLocation.MineralFields.OrderBy(m => worker.UnitCalculation.NearbyAllies.Count(a => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(a.Unit.Pos.X, a.Unit.Pos.Y)) < 3)).ThenBy(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(worker.UnitCalculation.Unit.Pos.X, worker.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
                    if (mineralField != null)
                    {
                        var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, mineralField.Tag);
                        if (action != null)
                        {
                            actions.Add(action);
                        }
                    }
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> TransferWorkers(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var oversaturatedBase = BaseManager.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.ResourceCenter.AssignedHarvesters > b.ResourceCenter.IdealHarvesters).OrderByDescending(b => b.ResourceCenter.AssignedHarvesters - b.ResourceCenter.IdealHarvesters).FirstOrDefault();
            if (oversaturatedBase != null)
            {
                var undersaturatedBase = BaseManager.SelfBases.Where(b => b.ResourceCenter.BuildProgress > .9 && b.ResourceCenter.AssignedHarvesters < b.ResourceCenter.IdealHarvesters).OrderBy(b => b.ResourceCenter.AssignedHarvesters - b.ResourceCenter.IdealHarvesters).FirstOrDefault();
                if (undersaturatedBase != null)
                {
                    var refinereries = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.BuildProgress >= .95f);
                    var worker = UnitCommanders.Where(c => c.UnitCalculation.Unit.Orders.Any(o => UnitDataManager.GatheringAbilities.Contains((Abilities)o.AbilityId) && !refinereries.Select(r => r.Key).Contains(o.TargetUnitTag))).OrderBy(w => Vector2.DistanceSquared(new Vector2(w.UnitCalculation.Unit.Pos.X, w.UnitCalculation.Unit.Pos.Y), new Vector2(oversaturatedBase.Location.X, oversaturatedBase.Location.Y))).FirstOrDefault();
                    var mineralField = undersaturatedBase.MineralFields.OrderBy(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(worker.UnitCalculation.Unit.Pos.X, worker.UnitCalculation.Unit.Pos.Y))).FirstOrDefault();
                    if (mineralField != null)
                    {
                        var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, mineralField.Tag);
                        if (action != null)
                        {
                            actions.Add(action);
                            return actions;
                        }
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
                    var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningAssignment.ResourceUnit.Tag);
                    if (action != null)
                    {
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

        List<SC2APIProtocol.Action> DealWithEnemies(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var selfBase in BaseManager.SelfBases)
            {
                if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Count() > 0 && !UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit) && a.TargetPriorityCalculation.GroundWinnability > 1))
                {
                    var enemyGroundDamage = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(e => e.DamageGround).Sum(e => e.Damage);
                    var commanders = UnitCommanders.Where(u => UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == u.UnitCalculation.Unit.Tag));
                    if (commanders.Count() < 1) { continue; }

                    if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(e => e.DamageGround || e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage) > enemyGroundDamage)
                    {
                        int desiredWorkers = 0;
                        var combatUnits = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));
                        var workers = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));
                        if (combatUnits.Count() == 0)
                        {
                            desiredWorkers = workers.Count() + 1;
                            if (workers.Count() > 8)
                            {
                                // TODO: this is a worker rush, set one defending worker as the bait and stay just out of range and run away
                            }
                        }
                        else
                        {
                            if (combatUnits.All(u => u.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING))
                            {
                                desiredWorkers = combatUnits.Count() * 2;
                            }
                            else
                            {
                                desiredWorkers = UnitCommanders.Count();
                            }
                        }

                        if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).Count() > 8)
                        {
                            // TODO: if all the workers are stacked, attack the closest enemy, check average position, if within 1 distance squared attack
                            var vectors = commanders.Select(c => new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y));
                            var averageVector = new Vector2(vectors.Average(v => v.X), vectors.Average(v => v.Y));
                            if (commanders.All(c => Vector2.DistanceSquared(averageVector, new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y)) < 1))
                            {
                                var closestEnemy = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), averageVector)).FirstOrDefault();
                                if (closestEnemy != null)
                                {
                                    var command = new ActionRawUnitCommand();
                                    foreach (var commander in commanders)
                                    {
                                        command.UnitTags.Add(commander.UnitCalculation.Unit.Tag);
                                    }
                                    command.AbilityId = (int)Abilities.ATTACK;
                                    command.TargetUnitTag = closestEnemy.Unit.Tag;

                                    var action = new SC2APIProtocol.Action
                                    {
                                        ActionRaw = new ActionRaw
                                        {
                                            UnitCommand = command
                                        }
                                    };
                                    actions.Add(action);
                                }
                            }
                            else
                            {
                                // spam a far mineral to bunch workers up until enemies get within range and then attack
                                var farMineral = selfBase.MineralFields.OrderByDescending(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                                if (farMineral != null)
                                {
                                    foreach (var commander in commanders)
                                    {
                                        var enemyInRange = commander.UnitCalculation.EnemiesInRange.OrderBy(e => e.Unit.Health + e.Unit.Shield).FirstOrDefault();
                                        if (enemyInRange != null && commander.UnitCalculation.Unit.WeaponCooldown == 0)
                                        {
                                            var action = commander.Order(frame, Abilities.ATTACK, null, enemyInRange.Unit.Tag);
                                            if (action != null)
                                            {
                                                actions.Add(action);
                                            }
                                        }
                                        else
                                        {
                                            var action = commander.Order(frame, Abilities.HARVEST_GATHER, null, farMineral.Tag, true);
                                            if (action != null)
                                            {
                                                actions.Add(action);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var commander in commanders)
                                    {
                                        var action = commander.Order(frame, Abilities.ATTACK, selfBase.Location);
                                        if (action != null)
                                        {
                                            actions.Add(action);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var enemy = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                            // TODO: maybe use a MicroController for this isntead

                            if (enemy != null)
                            {
                                int defendingCount = 0;
                                var command = new ActionRawUnitCommand();
                                foreach (var commander in commanders)
                                {
                                    if (defendingCount < desiredWorkers)
                                    {
                                        command.UnitTags.Add(commander.UnitCalculation.Unit.Tag);
                                        defendingCount++;
                                    }
                                }

                                if (defendingCount > 0)
                                {
                                    command.AbilityId = (int)Abilities.ATTACK;
                                    command.TargetWorldSpacePos = new Point2D { X = enemy.Unit.Pos.X, Y = enemy.Unit.Pos.Y };

                                    var action = new SC2APIProtocol.Action
                                    {
                                        ActionRaw = new ActionRaw
                                        {
                                            UnitCommand = command
                                        }
                                    };
                                    actions.Add(action);
                                }
                            }
                        }
                    }
                    else
                    {
                        // TODO: run
                    }
                }
            }

            // only defend near the base, don't chase too far
            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK) && !commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
                {
                    var action = commander.Order(frame, Abilities.MOVE, BaseManager.MainBase.Location);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }
            }

            return actions;
        }
    }
}
