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

        float NearbyDistance = 16;
        float AvoidRange = 1;

        ActiveUnitData ActiveUnitData;

        List<UnitTypes> UndeadTypes;

        int TargetPriorityCalculationFrame;

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

            TargetPriorityCalculationFrame = 0;

            UndeadTypes = new List<UnitTypes> { UnitTypes.ZERG_BROODLING, UnitTypes.ZERG_EGG, UnitTypes.ZERG_LARVA, UnitTypes.TERRAN_KD8CHARGE, UnitTypes.ZERG_OVERLORD, UnitTypes.ZERG_OVERLORDCOCOON, UnitTypes.ZERG_OVERLORDTRANSPORT, UnitTypes.ZERG_TRANSPORTOVERLORDCOCOON };
        }

        public override bool NeverSkip { get { return true; } }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            //var total = new Stopwatch();
            //total.Start();

            //var stopwatch = new Stopwatch();
            //stopwatch.Start();

            var frame = (int)observation.Observation.GameLoop;

            if (observation.Observation.RawData.Event != null && observation.Observation.RawData.Event.DeadUnits != null)
            {
                ActiveUnitData.DeadUnits = observation.Observation.RawData.Event.DeadUnits.ToList();
            }
            else
            {
                ActiveUnitData.DeadUnits = new List<ulong>();
            }

            foreach (var unit in ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR)) // remove things like purification novas that don't have dead unit events
            {
                if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                {
                    ActiveUnitData.DeadUnits.Add(unit.Key);
                }
            }
            foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR)) // remove things like purification novas that don't have dead unit events
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

            foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => UndeadTypes.Contains((UnitTypes)u.Value.Unit.UnitType)))
            {
                ActiveUnitData.EnemyUnits.TryRemove(unit.Key, out UnitCalculation removed);
            }
            foreach (var unit in ActiveUnitData.SelfUnits.Where(u => UndeadTypes.Contains((UnitTypes)u.Value.Unit.UnitType)))
            {
                ActiveUnitData.SelfUnits.TryRemove(unit.Key, out UnitCalculation removed);
            }
            foreach (var unit in ActiveUnitData.Commanders.Where(u => UndeadTypes.Contains((UnitTypes)u.Value.UnitCalculation.Unit.UnitType)))
            {
                ActiveUnitData.Commanders.TryRemove(unit.Key, out UnitCommander removed);
            }

            foreach (var unit in ActiveUnitData.NeutralUnits.Where(u => u.Value.Unit.DisplayType == DisplayType.Snapshot))
            {
                ActiveUnitData.NeutralUnits.TryRemove(unit.Key, out UnitCalculation removed);
            }

            //Debug.WriteLine($"removal {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

            var repairers = observation.Observation.RawData.Units.Where(u => u.UnitType == (uint)UnitTypes.TERRAN_SCV || u.UnitType == (uint)UnitTypes.TERRAN_MULE);

            //Parallel.ForEach(observation.Observation.RawData.Units, (unit) =>
            //{
            //    if (unit.Alliance == Alliance.Enemy)
            //    {
            //        var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
            //        var attack = new UnitCalculation(unit, repairingUnitCount, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.EnemyUnits[unit.Tag] = attack;
            //    }
            //    else if (unit.Alliance == Alliance.Self)
            //    {
            //        var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.SelfUnits[unit.Tag] = attack;
            //    }
            //    else if (unit.Alliance == Alliance.Neutral)
            //    {
            //        var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.NeutralUnits[unit.Tag] = attack;
            //    }
            //});

            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (unit.Alliance == Alliance.Enemy)
                {
                    var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
                    var attack = new UnitCalculation(unit, repairingUnitCount, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    ActiveUnitData.EnemyUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Self)
                {
                    var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    ActiveUnitData.SelfUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Neutral)
                {
                    var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
                    if (ActiveUnitData.NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    ActiveUnitData.NeutralUnits[unit.Tag] = attack;
                }
            }

            foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => u.Value.FrameLastSeen != frame && u.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure))) // structures get replaced by snapshots if we can't see them, so just remove them and let them get readded
            {
                ActiveUnitData.EnemyUnits.TryRemove(unit.Key, out UnitCalculation removed);
            }

            //Debug.WriteLine($"parallel {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

            foreach (var enemy in ActiveUnitData.EnemyUnits.Select(e => e.Value).ToList()) // if we can see this area of the map and the unit isn't there anymore remove it (we just remove it because visible units will get re-added below)
            {
                if (enemy.FrameLastSeen != frame && MapDataService.SelfVisible(enemy.Unit.Pos))
                {
                    ActiveUnitData.EnemyUnits.TryRemove(enemy.Unit.Tag, out UnitCalculation removed);
                }
            }

            //Debug.WriteLine($"remove vision {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

            foreach (var allyAttack in ActiveUnitData.SelfUnits)
            {
                foreach (var enemyAttack in ActiveUnitData.EnemyUnits)
                {
                    if (DamageService.CanDamage(allyAttack.Value, enemyAttack.Value) && Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Value.Position) <= (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        allyAttack.Value.EnemiesInRange.Add(enemyAttack.Value);
                        enemyAttack.Value.EnemiesInRangeOf.Add(allyAttack.Value);
                    }
                    if (DamageService.CanDamage(enemyAttack.Value, allyAttack.Value))
                    {
                        var distanceSquared = Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Value.Position);
                        if (distanceSquared <= (AvoidRange + enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (AvoidRange + enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                        {
                            allyAttack.Value.EnemiesInRangeOfAvoid.Add(enemyAttack.Value);
                            if (distanceSquared <= (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                            {
                                enemyAttack.Value.EnemiesInRange.Add(allyAttack.Value);
                                allyAttack.Value.EnemiesInRangeOf.Add(enemyAttack.Value);
                            }
                        }
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

            //Debug.WriteLine($"allyattack {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

            foreach (var enemyAttack in ActiveUnitData.EnemyUnits)
            {
                enemyAttack.Value.NearbyAllies = ActiveUnitData.EnemyUnits.Where(a => a.Key != enemyAttack.Key && Vector2.DistanceSquared(enemyAttack.Value.Position, a.Value.Position) <= NearbyDistance * NearbyDistance).Select(a => a.Value).ToList();
            }

            //Debug.WriteLine($"enemyunits {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

            if (TargetPriorityCalculationFrame + 10 < frame)
            {            
                foreach (var selfUnit in ActiveUnitData.SelfUnits)
                {
                    if (selfUnit.Value.TargetPriorityCalculation == null || selfUnit.Value.TargetPriorityCalculation.FrameCalculated + 10 < frame)
                    {
                        var priorityCalculation = TargetPriorityService.CalculateTargetPriority(selfUnit.Value, frame);
                        selfUnit.Value.TargetPriorityCalculation = priorityCalculation;
                        foreach (var nearbyUnit in selfUnit.Value.NearbyAllies.Where(a => a.NearbyEnemies.Count() == selfUnit.Value.NearbyAllies.Count()))
                        {
                            nearbyUnit.TargetPriorityCalculation = priorityCalculation;
                        }
                    }

                    selfUnit.Value.Attackers = GetTargettedAttacks(selfUnit.Value).ToList();
                }
                TargetPriorityCalculationFrame = frame;
            }
            //foreach (var selfUnit in ActiveUnitData.SelfUnits)
            //{
            //    if (selfUnit.Value.TargetPriorityCalculation == null || selfUnit.Value.TargetPriorityCalculation.FrameCalculated + 10 < frame)
            //    {
            //        var priorityCalculation = TargetPriorityService.CalculateTargetPriority(selfUnit.Value, frame);
            //        selfUnit.Value.TargetPriorityCalculation = priorityCalculation;
            //        foreach (var nearbyUnit in selfUnit.Value.NearbyAllies)
            //        {
            //            nearbyUnit.TargetPriorityCalculation = priorityCalculation;
            //        }
            //    }

            //    selfUnit.Value.Attackers = GetTargettedAttacks(selfUnit.Value).ToList();
            //}

            //Debug.WriteLine($"selfunits {stopwatch.ElapsedMilliseconds}");
            //stopwatch.Restart();

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

            //stopwatch.Stop();
            //Debug.WriteLine($"debug {stopwatch.ElapsedMilliseconds}");
            //total.Stop();
            //Debug.WriteLine($"total {total.ElapsedMilliseconds}");

            //if (total.ElapsedMilliseconds > 15)
            //{
            //    var uhoh = true;
            //}

            //Debug.WriteLine($"");

            return null;
        }

        ConcurrentBag<UnitCalculation> GetTargettedAttacks(UnitCalculation unitCalculation)
        {
            var attacks = new ConcurrentBag<UnitCalculation>();

            Parallel.ForEach(unitCalculation.EnemiesInRangeOf, (enemyAttack) =>
            {
                if (DamageService.CanDamage(enemyAttack, unitCalculation) && CollisionCalculator.Collides(unitCalculation.Position, unitCalculation.Unit.Radius, enemyAttack.Start, enemyAttack.End))
                {
                    attacks.Add(enemyAttack);
                }
            });

            return attacks;
        }
    }
}
