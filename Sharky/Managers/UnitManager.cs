using SC2APIProtocol;
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

        float NearbyDistance = 25;

        ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits;
        ConcurrentDictionary<ulong, UnitCalculation> SelfUnits;
        ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits;

        public ConcurrentDictionary<ulong, UnitCommander> Commanders { get; private set; }

        int EnemyDeaths;
        int SelfDeaths;
        int NeutralDeaths;

        public UnitManager(UnitDataManager unitDataManager, SharkyOptions sharkyOptions)
        {
            UnitDataManager = unitDataManager;
            SharkyOptions = sharkyOptions;

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
                foreach (ulong tag in observation.Observation.RawData.Event.DeadUnits)
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

            var repairers = observation.Observation.RawData.Units.Where(u => u.UnitType == (uint)UnitTypes.TERRAN_SCV || u.UnitType == (uint)UnitTypes.TERRAN_MULE);

            Parallel.ForEach(observation.Observation.RawData.Units, (unit) =>
            {
                if (unit.Alliance == Alliance.Enemy)
                {
                    var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
                    var attack = new UnitCalculation(unit, unit, repairingUnitCount, UnitDataManager, SharkyOptions);
                    EnemyUnits.AddOrUpdate(unit.Tag, attack, (tag, existingAttack) =>
                    {
                        attack.PreviousUnit = existingAttack.Unit;
                        return attack;
                    });
                }
                else if (unit.Alliance == Alliance.Self)
                {
                    var attack = new UnitCalculation(unit, unit, 0, UnitDataManager, SharkyOptions);
                    SelfUnits.AddOrUpdate(unit.Tag, attack, (tag, existingAttack) =>
                    {
                        attack.PreviousUnit = existingAttack.Unit;
                        return attack;
                    });
                }
            });

            foreach (var allyAttack in SelfUnits)
            {
                foreach (var enemyAttack in EnemyUnits)
                {
                    if (CanDamage(allyAttack.Value.Weapon, enemyAttack.Value.Unit) && Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(enemyAttack.Value.Unit.Pos.X, enemyAttack.Value.Unit.Pos.Y)) <= (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (allyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
                    {
                        allyAttack.Value.EnemiesInRange.Add(enemyAttack.Value);
                        enemyAttack.Value.EnemiesInRangeOf.Add(allyAttack.Value);
                    }
                    if (CanDamage(enemyAttack.Value.Weapon, allyAttack.Value.Unit) && Vector2.DistanceSquared(new Vector2(allyAttack.Value.Unit.Pos.X, allyAttack.Value.Unit.Pos.Y), new Vector2(enemyAttack.Value.Unit.Pos.X, enemyAttack.Value.Unit.Pos.Y)) <= (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius) * (enemyAttack.Value.Range + allyAttack.Value.Unit.Radius + enemyAttack.Value.Unit.Radius))
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

            return new List<SC2APIProtocol.Action>();
        }

        public int Count(UnitTypes unitType)
        {
            return SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType);
        }

        public int Completed(UnitTypes unitType)
        {
            return SelfUnits.Count(u => u.Value.Unit.UnitType == (uint)unitType && u.Value.Unit.BuildProgress == 1);
        }

        private bool CanDamage(Weapon weapon, Unit unit)
        {
            if (weapon == null || weapon.Damage == 0)
            {
                return false;
            }
            if ((unit.IsFlying || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)) && weapon.Type == Weapon.Types.TargetType.Ground)
            {
                return false;
            }
            if (!unit.IsFlying && weapon.Type == Weapon.Types.TargetType.Air && unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                return false;
            }
            return true;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
        }
    }
}
