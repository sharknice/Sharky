using SC2APIProtocol;
using Sharky.MicroControllers;
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
                if (ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag) && ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Count() > 0)
                {
                    if (!ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                    {
                        var enemyGroundDamage = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(e => e.DamageGround).Sum(e => e.Damage);
                        var nearbyWorkers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(a => a.UnitClassifications.Contains(UnitClassification.Worker));
                        var commanders = ActiveUnitData.Commanders.Where(c => nearbyWorkers.Any(w => w.Unit.Tag == c.Key));
                        if (commanders.Count() < 1) { continue; }

                        if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(e => e.DamageGround || e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage) > enemyGroundDamage)
                        {
                            int desiredWorkers = 0;
                            var combatUnits = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));
                            var workers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));

                            desiredWorkers = 1;
                            if (workers.Count() > 1)
                            {
                                desiredWorkers = workers.Count() + 1;
                            }
                            if (workers.Count() > 8) // this is a worker rush, set one defending worker as the bait that will stay just out of range and run away
                            {
                                var bait = commanders.Any(w => w.Value.UnitRole == UnitRole.Bait);
                                if (!bait && commanders.Count() > 0)
                                {
                                    commanders.FirstOrDefault().Value.UnitRole = UnitRole.Bait;
                                }
                            }

                            if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).Count() > 8)
                            {
                                // if all the workers are stacked, attack the closest enemy
                                var selectedCommanders = commanders.Where(c => c.Value.UnitRole != UnitRole.Bait);
                                var vectors = selectedCommanders.Select(c => c.Value.UnitCalculation.Position);
                                var averageVector = new Vector2(vectors.Average(v => v.X), vectors.Average(v => v.Y));
                                if (selectedCommanders.All(c => Vector2.DistanceSquared(averageVector, c.Value.UnitCalculation.Position) < 1))
                                {
                                    var closestEnemy = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(u.Position, averageVector)).FirstOrDefault();
                                    if (closestEnemy != null)
                                    {
                                        var command = new ActionRawUnitCommand();
                                        foreach (var commander in selectedCommanders)
                                        {
                                            if (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.Gas)
                                            {
                                                commander.Value.UnitRole = UnitRole.Defend;
                                            }
                                            command.UnitTags.Add(commander.Value.UnitCalculation.Unit.Tag);
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
                                    // spam a pocket mineral to bunch workers up until enemies get within range and then attack
                                    var farMineral = selfBase.MineralFields.Where(m => selfBase.MineralFields.Count(o => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(o.Pos.X, o.Pos.Y)) < 4) > 2).OrderByDescending(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                                    if (farMineral != null)
                                    {
                                        foreach (var commander in selectedCommanders)
                                        {
                                            if (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.Gas)
                                            {
                                                commander.Value.UnitRole = UnitRole.Defend;
                                            }

                                            var enemyInRange = commander.Value.UnitCalculation.EnemiesInRange.OrderBy(e => e.Unit.Health + e.Unit.Shield).FirstOrDefault();
                                            if (enemyInRange != null && commander.Value.UnitCalculation.Unit.WeaponCooldown == 0)
                                            {
                                                var action = commander.Value.Order(frame, Abilities.ATTACK, null, enemyInRange.Unit.Tag);
                                                if (action != null)
                                                {
                                                    actions.AddRange(action);
                                                }
                                            }
                                            else
                                            {
                                                var action = commander.Value.Order(frame, Abilities.HARVEST_GATHER, null, farMineral.Tag, true);
                                                if (action != null)
                                                {
                                                    actions.AddRange(action);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var commander in selectedCommanders)
                                        {
                                            if (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.Gas)
                                            {
                                                commander.Value.UnitRole = UnitRole.Defend;
                                            }

                                            var action = commander.Value.Order(frame, Abilities.ATTACK, selfBase.Location);
                                            if (action != null)
                                            {
                                                actions.AddRange(action);
                                            }
                                        }
                                    }
                                }

                                foreach (var commander in commanders.Where(c => c.Value.UnitRole == UnitRole.Bait))
                                {
                                    var action = WorkerMicroController.Bait(commander.Value, BaseData.BaseLocations.Last().Location, BaseData.BaseLocations.First().Location, null, frame);
                                    if (action != null)
                                    {
                                        actions.AddRange(action);
                                    }
                                }
                            }
                            else
                            {
                                var enemy = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(u.Position, new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                                // TODO: maybe use a MicroController for this isntead
                                if (commanders.Count() < desiredWorkers)
                                {
                                    actions.AddRange(Run(frame, unitCommanders, selfBase));
                                }
                                else if (enemy != null)
                                {
                                    while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers)
                                    {
                                        commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend).Value.UnitRole = UnitRole.Defend;
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
            }

            var safeWorkers = ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && (c.Value.UnitCalculation.EnemiesInRangeOf.Count() == 0 || (c.Value.UnitCalculation.Unit.Health == c.Value.UnitCalculation.Unit.HealthMax && c.Value.UnitCalculation.Unit.Shield == c.Value.UnitCalculation.Unit.ShieldMax)) && c.Value.UnitRole == UnitRole.Defend || c.Value.UnitRole == UnitRole.Bait);
            foreach (var safeWorker in safeWorkers)
            {
                if (safeWorker.Value.UnitRole == UnitRole.Bait && safeWorker.Value.UnitCalculation.NearbyEnemies.Count() > 0)
                {
                    continue;
                }
                safeWorker.Value.UnitRole = UnitRole.None;
            }

            // only defend near the base, don't chase too far
            foreach (var commander in unitCommanders)
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
                }

            }

            return actions;
        }
    }
}
