using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Sharky.Managers
{
    public class UnitManager : SharkyManager
    {
        SharkyUnitData SharkyUnitData;
        SharkyOptions SharkyOptions;
        TargetPriorityService TargetPriorityService;
        CollisionCalculator CollisionCalculator;
        MapDataService MapDataService;
        DebugService DebugService;
        DamageService DamageService;
        UnitDataService UnitDataService;

        float NearbyDistance = 25;

        ActiveUnitData ActiveUnitData;

        public UnitManager(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, SharkyOptions sharkyOptions, TargetPriorityService targetPriorityService, CollisionCalculator collisionCalculator, MapDataService mapDataService, DebugService debugService, DamageService damageService, UnitDataService unitDataService)
        {
            ActiveUnitData = activeUnitData;

            SharkyUnitData = sharkyUnitData;
            SharkyOptions = sharkyOptions;
            TargetPriorityService = targetPriorityService;
            CollisionCalculator = collisionCalculator;
            MapDataService = mapDataService;
            DebugService = debugService;
            DamageService = damageService;
            UnitDataService = unitDataService;

            ActiveUnitData.EnemyUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            ActiveUnitData.SelfUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            ActiveUnitData.NeutralUnits = new ConcurrentDictionary<ulong, UnitCalculation>();

            ActiveUnitData.Commanders = new ConcurrentDictionary<ulong, UnitCommander>();

            ActiveUnitData.DeadUnits = new List<ulong>();
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            if (observation.Observation.RawData.Event != null && observation.Observation.RawData.Event.DeadUnits != null)
            {
                ActiveUnitData.DeadUnits = observation.Observation.RawData.Event.DeadUnits.ToList();
            }
            else
            {
                ActiveUnitData.DeadUnits = new List<ulong>();
            }

            foreach (var unit in ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED)) // remove things like purification novas that don't have dead unit events
            {
                if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                {
                    ActiveUnitData.DeadUnits.Add(unit.Key);
                }
            }

            foreach (var tag in ActiveUnitData.DeadUnits)
            {
                if (ActiveUnitData.EnemyUnits.TryRemove(tag, out UnitCalculation removedEnemy))
                {
                    ActiveUnitData.EnemyDeaths++;
                }
                else if (ActiveUnitData.SelfUnits.TryRemove(tag, out UnitCalculation removedAlly))
                {
                    ActiveUnitData.SelfDeaths++;
                }
                else if (ActiveUnitData.NeutralUnits.TryRemove(tag, out UnitCalculation removedNeutral))
                {
                    ActiveUnitData.NeutralDeaths++;
                }

                ActiveUnitData.Commanders.TryRemove(tag, out UnitCommander removedCommander);
            }

            var enemyList = ActiveUnitData.EnemyUnits.Select(e => e.Value).ToList();

            foreach (var enemy in enemyList) // if we can see this area of the map and the unit isn't there anymore remove it (we just remove it because visible units will get re-added below)
            {
                if (MapDataService.SelfVisible(enemy.Unit.Pos))
                {
                    ActiveUnitData.EnemyUnits.TryRemove(enemy.Unit.Tag, out UnitCalculation removed);
                }
                else if (enemy.Attributes.Contains(Attribute.Structure)) // structures get replaced by snapshots if we can't see them, so just remove them and let them get readded
                {
                    ActiveUnitData.EnemyUnits.TryRemove(enemy.Unit.Tag, out UnitCalculation removed);
                }
            }

            var repairers = observation.Observation.RawData.Units.Where(u => u.UnitType == (uint)UnitTypes.TERRAN_SCV || u.UnitType == (uint)UnitTypes.TERRAN_MULE);

            Parallel.ForEach(observation.Observation.RawData.Units, (unit) =>
            {
                if (unit.UnitType == (uint)UnitTypes.TERRAN_KD8CHARGE)
                {

                }
                else if (unit.Alliance == Alliance.Enemy)
                {
                    var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
                    var attack = new UnitCalculation(unit, unit, repairingUnitCount, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing.Unit);
                    }
                    ActiveUnitData.EnemyUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Self)
                {
                    var attack = new UnitCalculation(unit, unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing.Unit);
                    }
                    ActiveUnitData.SelfUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Neutral)
                {
                    var attack = new UnitCalculation(unit, unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing.Unit);
                    }
                    ActiveUnitData.NeutralUnits[unit.Tag] = attack;
                }
            });

            foreach (var allyAttack in ActiveUnitData.SelfUnits)
            {
                foreach (var enemyAttack in ActiveUnitData.EnemyUnits)
                {
                    if (DamageService.CanDamage(allyAttack.Value.Weapons, enemyAttack.Value.Unit) && Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Value.Position) <= (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        allyAttack.Value.EnemiesInRange.Add(enemyAttack.Value);
                        enemyAttack.Value.EnemiesInRangeOf.Add(allyAttack.Value);
                    }
                    if (DamageService.CanDamage(enemyAttack.Value.Weapons, allyAttack.Value.Unit) && Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Value.Position) <= (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        enemyAttack.Value.EnemiesInRange.Add(allyAttack.Value);
                        allyAttack.Value.EnemiesInRangeOf.Add(enemyAttack.Value);
                    }

                    if (Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Value.Position) <= NearbyDistance * NearbyDistance)
                    {
                        enemyAttack.Value.NearbyEnemies.Add(allyAttack.Value);
                        allyAttack.Value.NearbyEnemies.Add(enemyAttack.Value);
                    }
                }

                allyAttack.Value.NearbyAllies = ActiveUnitData.SelfUnits.Where(a => a.Key != allyAttack.Key && Vector2.DistanceSquared(allyAttack.Value.Position, a.Value.Position) <= NearbyDistance * NearbyDistance).Select(a => a.Value).ToList();

                var commander = new UnitCommander(allyAttack.Value);
                ActiveUnitData.Commanders.AddOrUpdate(allyAttack.Value.Unit.Tag, commander, (tag, existingCommander) =>
                {
                    commander = existingCommander;
                    commander.UnitCalculation = allyAttack.Value;
                    return commander;
                });
            }

            foreach (var selfUnit in ActiveUnitData.SelfUnits)
            {
                if (selfUnit.Value.TargetPriorityCalculation == null)
                {
                    var priorityCalculation = TargetPriorityService.CalculateTargetPriority(selfUnit.Value);
                    selfUnit.Value.TargetPriorityCalculation = priorityCalculation;
                    foreach (var nearbyUnit in selfUnit.Value.NearbyAllies)
                    {
                        nearbyUnit.TargetPriorityCalculation = priorityCalculation;
                    }
                }

                selfUnit.Value.Attackers = GetTargettedAttacks(selfUnit.Value).ToList();
            }

            if (SharkyOptions.Debug)
            {
                foreach (var selfUnit in ActiveUnitData.SelfUnits)
                {
                    DebugService.DrawLine(selfUnit.Value.Unit.Pos, new Point { X = selfUnit.Value.End.X, Y = selfUnit.Value.End.Y, Z = selfUnit.Value.Unit.Pos.Z + 1f }, new SC2APIProtocol.Color { R = 0, B = 0, G = 255 });
                }
                foreach (var enemyUnit in ActiveUnitData.EnemyUnits)
                {
                    DebugService.DrawLine(enemyUnit.Value.Unit.Pos, new Point { X = enemyUnit.Value.End.X, Y = enemyUnit.Value.End.Y, Z = enemyUnit.Value.Unit.Pos.Z + 1f }, new SC2APIProtocol.Color { R = 255, B = 0, G = 0 });
                }
            }

            return new List<SC2APIProtocol.Action>();
        }

        ConcurrentBag<UnitCalculation> GetTargettedAttacks(UnitCalculation unitCalculation)
        {
            var attacks = new ConcurrentBag<UnitCalculation>();

            Parallel.ForEach(unitCalculation.EnemiesInRangeOf, (enemyAttack) =>
            {
                if (DamageService.CanDamage(enemyAttack.Weapons, unitCalculation.Unit) && CollisionCalculator.Collides(unitCalculation.Position, unitCalculation.Unit.Radius, enemyAttack.Start, enemyAttack.End))
                {
                    attacks.Add(enemyAttack);
                }
            });

            return attacks;
        }
    }
}
