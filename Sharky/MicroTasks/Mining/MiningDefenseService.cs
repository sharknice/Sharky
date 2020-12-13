using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroControllers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Mining
{
    public class MiningDefenseService
    {
        IBaseManager BaseManager;
        IUnitManager UnitManager;
        IIndividualMicroController WorkerMicroController;
        DebugManager DebugManager;

        public MiningDefenseService(IBaseManager baseManager, IUnitManager unitManager, IIndividualMicroController workerMicroController, DebugManager debugManager)
        {
            BaseManager = baseManager;
            UnitManager = unitManager;
            WorkerMicroController = workerMicroController;
            DebugManager = debugManager;
        }

        public List<SC2APIProtocol.Action> DealWithEnemies(int frame, List<UnitCommander> unitCommanders)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var selfBase in BaseManager.SelfBases)
            {
                if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Count() > 0 && !UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)))
                {
                    var enemyGroundDamage = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(e => e.DamageGround).Sum(e => e.Damage);
                    var commanders = unitCommanders.Where(u => UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == u.UnitCalculation.Unit.Tag));
                    if (commanders.Count() < 1) { continue; }

                    if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(e => e.DamageGround || e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage) > enemyGroundDamage)
                    {
                        int desiredWorkers = 0;
                        var combatUnits = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));
                        var workers = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));
                        if (combatUnits.Count() == 0)
                        {
                            desiredWorkers = workers.Count() + 1;
                            if (workers.Count() > 8) // this is a worker rush, set one defending worker as the bait that will stay just out of range and run away
                            {
                                var bait = commanders.Where(w => w.UnitRole == UnitRole.Bait).FirstOrDefault();
                                if (bait == null && commanders.Count() > 0)
                                {
                                    commanders.FirstOrDefault().UnitRole = UnitRole.Bait;
                                }
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
                                desiredWorkers = combatUnits.Count() * 3;
                            }
                        }

                        if (UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).Count() > 8)
                        {
                            // if all the workers are stacked, attack the closest enemy
                            var selectedCommanders = commanders.Where(c => c.UnitRole != UnitRole.Bait);
                            var vectors = selectedCommanders.Select(c => new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y));
                            var averageVector = new Vector2(vectors.Average(v => v.X), vectors.Average(v => v.Y));
                            if (selectedCommanders.All(c => Vector2.DistanceSquared(averageVector, new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y)) < 1))
                            {
                                var closestEnemy = UnitManager.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), averageVector)).FirstOrDefault();
                                if (closestEnemy != null)
                                {
                                    var command = new ActionRawUnitCommand();
                                    foreach (var commander in selectedCommanders)
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
                                    foreach (var commander in selectedCommanders)
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
                                    foreach (var commander in selectedCommanders)
                                    {
                                        var action = commander.Order(frame, Abilities.ATTACK, selfBase.Location);
                                        if (action != null)
                                        {
                                            actions.Add(action);
                                        }
                                    }
                                }
                            }

                            foreach (var commander in commanders.Where(c => c.UnitRole == UnitRole.Bait))
                            {
                                var action = WorkerMicroController.Bait(commander, BaseManager.BaseLocations.Last().Location, BaseManager.BaseLocations.First().Location, null, frame);
                                if (action != null)
                                {
                                    actions.Add(action);
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
            foreach (var commander in unitCommanders)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK) && (!commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)) || commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
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
