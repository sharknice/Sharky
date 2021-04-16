using SC2APIProtocol;
using Sharky.MicroControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Mining
{
    public class MiningDefenseService
    {
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        IIndividualMicroController WorkerMicroController;
        DebugService DebugService;

        public MiningDefenseService(BaseData baseData, ActiveUnitData activeUnitData, IIndividualMicroController workerMicroController, DebugService debugService)
        {
            BaseData = baseData;
            ActiveUnitData = activeUnitData;
            WorkerMicroController = workerMicroController;
            DebugService = debugService;
        }

        public List<SC2APIProtocol.Action> DealWithEnemies(int frame, List<UnitCommander> unitCommanders)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var selfBase in BaseData.SelfBases)
            {
                bool preventGasSteal = false;
                if (ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag) && ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Count() > 0)
                {
                    if (!ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) || e.Attributes.Contains(SC2APIProtocol.Attribute.Structure))))
                    {
                        var enemyGroundDamage = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(e => e.DamageGround).Sum(e => e.Damage);
                        var nearbyWorkers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(a => a.UnitClassifications.Contains(UnitClassification.Worker));
                        var commanders = ActiveUnitData.Commanders.Where(c => nearbyWorkers.Any(w => w.Unit.Tag == c.Key));
                        if (commanders.Count() < 1) { continue; }

                        if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(e => e.DamageGround || e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage) > enemyGroundDamage || BaseData.SelfBases.Count() == 1)
                        {
                            int desiredWorkers = 0;
                            var combatUnits = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));
                            var workers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));

                            desiredWorkers = 1;
                            if (workers.Count(w => w.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE) > 0)
                            {      
                                var takenGases = selfBase.GasMiningInfo.Select(i => i.ResourceUnit);
                                var openGeysers = BaseData.BaseLocations.SelectMany(b => b.VespeneGeysers).Where(g => g.VespeneContents > 0 && !takenGases.Any(t => t.Pos.X == g.Pos.X && t.Pos.Y == g.Pos.Y));
                                foreach (var gas in openGeysers)
                                {
                                    foreach (var worker in workers)
                                    {
                                        if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), worker.Position) < 64)
                                        {
                                            preventGasSteal = true; // If enemy probe looking at empty gas send closest worker to touch that gas and prevent a steal
                                            var closestDefender = commanders.OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, new Vector2(gas.Pos.X, gas.Pos.Y))).FirstOrDefault();
                                            closestDefender.Value.UnitRole = UnitRole.PreventGasSteal;
                                            var action = closestDefender.Value.Order(frame, Abilities.MOVE, new SC2APIProtocol.Point2D { X = gas.Pos.X, Y = gas.Pos.Y });
                                            if (action != null)
                                            {
                                                actions.AddRange(action);
                                            }
                                        }
                                    }
                                }
                            }
                            if (workers.Count() > 1)
                            {
                                desiredWorkers = workers.Count() + 1;
                            }
                            if (workers.Count() > 8) // this is a worker rush
                            {
                                desiredWorkers = workers.Count() + 3;
                            }
                            if (desiredWorkers > commanders.Count())
                            {
                                desiredWorkers = commanders.Count();
                            }

                            if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).Count() > 8)
                            {
                                DebugService.DrawText("--------------Defending Worker Rush-------------");
                                while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers)
                                {
                                    commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value.UnitRole = UnitRole.Defend;
                                }
                                var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                foreach (var defender in defenders)
                                {
                                    actions.AddRange(WorkerMicroController.Attack(defender.Value, selfBase.Location, selfBase.MineralLineLocation, null, frame));
                                }
                                DebugService.DrawText($"--------------{defenders.Count()} versus {workers.Count()}-------------");

                            }
                            else
                            {
                                var baseVector = new Vector2(selfBase.Location.X, selfBase.Location.Y);
                                var enemyBuildings = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderByDescending(u => u.Unit.BuildProgress).ThenBy(u => Vector2.DistanceSquared(u.Position, baseVector));
                                desiredWorkers = (enemyBuildings.Count() * 4) + workers.Count();

                                var enemy = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(u.Position, new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                                if (enemyBuildings.Count() > 0 && !enemyBuildings.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && u.Unit.Shield == u.Unit.ShieldMax && u.Unit.BuildProgress == 1))
                                { // TODO: test this
                                    while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers && commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < commanders.Count())
                                    {
                                        commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value.UnitRole = UnitRole.Defend;
                                    }
                                    var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                    var sentWorkers = new List<ulong>();
                                    foreach (var enemyBuilding in enemyBuildings)
                                    {
                                        var closestDefenders = defenders.Where(d => !sentWorkers.Contains(d.Key)).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, enemyBuilding.Position)).Take(4); // attack each building with 4 workers
                                        foreach (var defender in closestDefenders)
                                        {
                                            var action = defender.Value.Order(frame, Abilities.ATTACK, null, enemyBuilding.Unit.Tag);
                                            if (action != null)
                                            {
                                                actions.AddRange(action);
                                            }
                                        }
                                        sentWorkers.AddRange(closestDefenders.Select(d => d.Key));
                                    }
                                    foreach (var enemyWorker in workers.OrderBy(u => Vector2.DistanceSquared(u.Position, baseVector)))
                                    {
                                        var closestDefenders = defenders.Where(d => !sentWorkers.Contains(d.Key)).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, enemyWorker.Position)).Take(1);
                                        foreach (var defender in closestDefenders)
                                        {
                                            var action = defender.Value.Order(frame, Abilities.ATTACK, null, enemyWorker.Unit.Tag);
                                            if (action != null)
                                            {
                                                actions.AddRange(action);
                                            }
                                        }
                                        sentWorkers.AddRange(closestDefenders.Select(d => d.Key));
                                    }
                                }
                                else if (commanders.Count() < desiredWorkers)
                                {
                                    actions.AddRange(Run(frame, unitCommanders, selfBase));
                                }
                                else if (enemy != null)
                                {
                                    while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers && commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < commanders.Count())
                                    {
                                        commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value.UnitRole = UnitRole.Defend;
                                    }

                                    var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                    foreach (var defender in defenders)
                                    {
                                        actions.AddRange(WorkerMicroController.Attack(defender.Value, selfBase.Location, selfBase.MineralLineLocation, null, frame));
                                    }
                                }
                            }
                        }
                        else
                        {
                            actions.AddRange(Run(frame, unitCommanders, selfBase));
                        }
                    }
                    else
                    {
                        actions.AddRange(Run(frame, unitCommanders, selfBase));
                    }
                }
                if (!preventGasSteal)
                {
                    foreach (var commander in unitCommanders.Where(c => c.UnitRole == UnitRole.PreventGasSteal))
                    {
                        commander.UnitRole = UnitRole.None;
                    }
                }
            }

            var safeWorkers = ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && (c.Value.UnitRole == UnitRole.Defend || c.Value.UnitRole == UnitRole.Bait) && c.Value.UnitCalculation.Unit.Orders.Count() == 0);
            foreach (var safeWorker in safeWorkers)
            {
                if (safeWorker.Value.UnitRole == UnitRole.Bait && safeWorker.Value.UnitCalculation.NearbyEnemies.Count() > 0)
                {
                    continue;
                }
                safeWorker.Value.UnitRole = UnitRole.None;
            }

            // only defend near the base, don't chase too far
            foreach (var commander in unitCommanders.Where(u => u.UnitRole == UnitRole.Defend))
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK) && (!commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)) || commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
                {
                    var action = commander.Order(frame, Abilities.STOP);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
        }

        private List<SC2APIProtocol.Action> Run(int frame, List<UnitCommander> unitCommanders, BaseLocation selfBase)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var otherBase = BaseData.BaseLocations.FirstOrDefault(b => b.Location.X != selfBase.Location.X && b.Location.Y != selfBase.Location.Y);
            if (otherBase != null)
            {
                foreach (var commander in unitCommanders)
                {
                    if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0 && (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax || commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax))
                    {
                        if (commander.UnitRole == UnitRole.Minerals || commander.UnitRole == UnitRole.Gas)
                        {
                            commander.UnitRole = UnitRole.Defend;
                        }

                        if (commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.EnemiesInRangeOf.First().Damage || commander.UnitCalculation.UnitTypeData.MovementSpeed < commander.UnitCalculation.EnemiesInRangeOf.First().UnitTypeData.MovementSpeed)
                        {
                            var action = WorkerMicroController.Retreat(commander, otherBase.MineralLineLocation, null, frame);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                        else
                        {
                            var action = WorkerMicroController.Bait(commander, BaseData.BaseLocations.Last().Location, BaseData.BaseLocations.First().Location, null, frame);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }
                    else if (commander.UnitCalculation.Unit.WeaponCooldown == 0 && commander.UnitCalculation.EnemiesInRange.Count() > 0) // TODO: test this, attack any units if they walk by
                    {
                        var action = commander.Order(frame, Abilities.ATTACK, null, commander.UnitCalculation.EnemiesInRange.OrderBy(e => e.Unit.Health + e.Unit.Shield).FirstOrDefault().Unit.Tag);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                }

            }

            return actions;
        }
    }
}
