using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Sharky.Managers
{
    public class UnitManager : IUnitManager
    {
        UnitDataManager UnitDataManager;
        SharkyOptions SharkyOptions;
        TargetPriorityService TargetPriorityService;
        CollisionCalculator CollisionCalculator;
        MapDataService MapDataService;

        float NearbyDistance = 25;

        public ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits { get; private set; }
        public ConcurrentDictionary<ulong, UnitCalculation> SelfUnits { get; private set; }
        public ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits { get; private set; }

        public ConcurrentDictionary<ulong, UnitCommander> Commanders { get; private set; }

        int EnemyDeaths;
        int SelfDeaths;
        int NeutralDeaths;

        public UnitManager(UnitDataManager unitDataManager, SharkyOptions sharkyOptions, TargetPriorityService targetPriorityService, CollisionCalculator collisionCalculator, MapDataService mapDataService)
        {
            UnitDataManager = unitDataManager;
            SharkyOptions = sharkyOptions;
            TargetPriorityService = targetPriorityService;
            CollisionCalculator = collisionCalculator;
            MapDataService = mapDataService;

            EnemyUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            SelfUnits = new ConcurrentDictionary<ulong, UnitCalculation>();
            NeutralUnits = new ConcurrentDictionary<ulong, UnitCalculation>();

            Commanders = new ConcurrentDictionary<ulong, UnitCommander>();

            EnemyDeaths = 0;
            SelfDeaths = 0;
            NeutralDeaths = 0;
        }

        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (observation.Observation.RawData.Event != null && observation.Observation.RawData.Event.DeadUnits != null)
            {
                foreach (var tag in observation.Observation.RawData.Event.DeadUnits)
                {
                    if (EnemyUnits.TryRemove(tag, out UnitCalculation removedEnemy))
                    {
                        EnemyDeaths++;
                    }
                    else if (SelfUnits.TryRemove(tag, out UnitCalculation removedAlly))
                    {
                        SelfDeaths++;
                    }
                    else if (NeutralUnits.TryRemove(tag, out UnitCalculation removedNeutral))
                    {
                        NeutralDeaths++;
                    }

                    Commanders.TryRemove(tag, out UnitCommander removedCommander);
                }
            }

            foreach (var enemy in EnemyUnits.Select(e => e.Value).ToList()) // if we can see this area of the map and the unit isn't there anymore remove it (we just remove it because visible units will get re-added below)
            {
                if (MapDataService.SelfVisible(enemy.Unit.Pos))
                {
                    EnemyUnits.TryRemove(enemy.Unit.Tag, out UnitCalculation removed);
                }
            }

            var repairers = observation.Observation.RawData.Units.Where(u => u.UnitType == (uint)UnitTypes.TERRAN_SCV || u.UnitType == (uint)UnitTypes.TERRAN_MULE);

            Parallel.ForEach(observation.Observation.RawData.Units, (unit) =>
            {
                if (unit.Alliance == Alliance.Enemy)
                {
                    var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
                    var attack = new UnitCalculation(unit, unit, repairingUnitCount, UnitDataManager, SharkyOptions);
                    if (EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.PreviousUnit = existing.Unit;
                    }
                    EnemyUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Self)
                {
                    var attack = new UnitCalculation(unit, unit, 0, UnitDataManager, SharkyOptions);
                    if (SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.PreviousUnit = existing.Unit;
                    }
                    SelfUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Neutral)
                {
                    var attack = new UnitCalculation(unit, unit, 0, UnitDataManager, SharkyOptions);
                    if (NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.PreviousUnit = existing.Unit;
                    }
                    NeutralUnits[unit.Tag] = attack;
                }
            });

            foreach (var allyAttack in SelfUnits)
            {
                foreach (var enemyAttack in EnemyUnits)
                {
                    if (CanDamage(allyAttack.Value.Weapons, enemyAttack.Value.Unit) && Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(enemyAttack.Value.Unit.Pos.X, enemyAttack.Value.Unit.Pos.Y)) <= (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        allyAttack.Value.EnemiesInRange.Add(enemyAttack.Value);
                        enemyAttack.Value.EnemiesInRangeOf.Add(allyAttack.Value);
                    }
                    if (CanDamage(enemyAttack.Value.Weapons, allyAttack.Value.Unit) && Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(enemyAttack.Value.Unit.Pos.X, enemyAttack.Value.Unit.Pos.Y)) <= (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        enemyAttack.Value.EnemiesInRange.Add(allyAttack.Value);
                        allyAttack.Value.EnemiesInRangeOf.Add(enemyAttack.Value);
                    }

                    if (Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(enemyAttack.Value.Unit.Pos.X, enemyAttack.Value.Unit.Pos.Y)) <= NearbyDistance * NearbyDistance)
                    {
                        enemyAttack.Value.NearbyEnemies.Add(allyAttack.Value);
                        allyAttack.Value.NearbyEnemies.Add(enemyAttack.Value);
                    }
                }

                allyAttack.Value.NearbyAllies = SelfUnits.Where(a => a.Key != allyAttack.Key && Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(a.Value.Unit.Pos.X, a.Value.Unit.Pos.Y)) <= NearbyDistance * NearbyDistance).Select(a => a.Value).ToList();

                var commander = new UnitCommander(allyAttack.Value);
                Commanders.AddOrUpdate(allyAttack.Value.Unit.Tag, commander, (tag, existingCommander) =>
                {
                    commander = existingCommander;
                    commander.UnitCalculation = allyAttack.Value;
                    return commander;
                });
            }

            foreach (var selfUnit in SelfUnits)
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

                //selfUnit.Value.Attackers = new List<UnitCalculation>();
                selfUnit.Value.Attackers = GetTargettedAttacks(selfUnit.Value).ToList();
            }

            return new List<SC2APIProtocol.Action>();
        }

        ConcurrentBag<UnitCalculation> GetTargettedAttacks(UnitCalculation unitCalculation)
        {
            var attacks = new ConcurrentBag<UnitCalculation>();
            var center = new Vector2(unitCalculation.Unit.Pos.X, unitCalculation.Unit.Pos.Y);

            Parallel.ForEach(unitCalculation.EnemiesInRangeOf, (enemyAttack) =>
            {
                if (CanDamage(enemyAttack.Weapons, unitCalculation.Unit) && CollisionCalculator.Collides(center, unitCalculation.Unit.Radius, enemyAttack.Start, enemyAttack.End))
                {
                    attacks.Add(enemyAttack);
                }
            });

            return attacks;
        }

        public int Count(UnitTypes unitType)
        {
            return SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType);
        }

        public int EnemyCount(UnitTypes unitType)
        {
            return EnemyUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType);
        }

        public int UnitsInProgressCount(UnitTypes unitType)
        {
            var unitData = UnitDataManager.TrainingData[unitType];
            return SelfUnits.Count(u => (unitData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) || u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_EGG) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)unitData.Ability));
        }

        public int EquivalentTypeCount(UnitTypes unitType)
        {
            var count = Count(unitType);
            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                count += Count(UnitTypes.PROTOSS_WARPGATE);
            }
            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                count += Count(UnitTypes.ZERG_HIVE);
                count += Count(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                count += Count(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                count += Count(UnitTypes.TERRAN_ORBITALCOMMAND);
                count += Count(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                count += Count(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_ORBITALCOMMAND)
            {
                count += Count(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                count += Count(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                count += Count(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                count += Count(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                count += Count(UnitTypes.TERRAN_STARPORTFLYING);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                count += Count(UnitTypes.ZERG_GREATERSPIRE);
            }

            return count;
        }

        public int Completed(UnitTypes unitType)
        {
            return SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType && u.Value.Unit.BuildProgress == 1);
        }

        public int EquivalentTypeCompleted(UnitTypes unitType)
        {
            var completed = Completed(unitType);
            if (unitType == UnitTypes.PROTOSS_GATEWAY)
            {
                completed += Completed(UnitTypes.PROTOSS_WARPGATE);
            }
            else if (unitType == UnitTypes.ZERG_HATCHERY)
            {
                completed += Completed(UnitTypes.ZERG_HIVE);
                completed += Completed(UnitTypes.ZERG_LAIR);
            }
            else if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                completed += Completed(UnitTypes.TERRAN_COMMANDCENTERFLYING);
                completed += Completed(UnitTypes.TERRAN_ORBITALCOMMAND);
                completed += Completed(UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
                completed += Completed(UnitTypes.TERRAN_PLANETARYFORTRESS);
            }
            else if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                completed += Completed(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
            }
            else if (unitType == UnitTypes.TERRAN_BARRACKS)
            {
                completed += Completed(UnitTypes.TERRAN_BARRACKSFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_FACTORY)
            {
                completed += Completed(UnitTypes.TERRAN_FACTORYFLYING);
            }
            else if (unitType == UnitTypes.TERRAN_STARPORT)
            {
                completed += Completed(UnitTypes.TERRAN_STARPORTFLYING);
            }
            else if (unitType == UnitTypes.ZERG_SPIRE)
            {
                completed += Completed(UnitTypes.ZERG_GREATERSPIRE);
            }

            return completed;
        }

        public bool CanDamage(IEnumerable<Weapon> weapons, Unit unit)
        {
            if (weapons.Count() == 0 || weapons.All(w => w.Damage == 0))
            {
                return false;
            }
            if ((unit.IsFlying || unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)) && weapons.Any(w => w.Type == Weapon.Types.TargetType.Air || w.Type == Weapon.Types.TargetType.Any))
            {
                return true;
            }
            if (!unit.IsFlying && weapons.Any(w => w.Type == Weapon.Types.TargetType.Ground || w.Type == Weapon.Types.TargetType.Any))
            {
                return true;
            }
            return false;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
        }
    }
}
