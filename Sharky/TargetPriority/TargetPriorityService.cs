using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky
{
    public class TargetPriorityService
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;

        public TargetPriorityService(DefaultSharkyBot defaultSharkyBot)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
        }

        public TargetPriorityCalculation CalculateTargetPriority(UnitCalculation unitCalculation, int frame)
        {
            var allies = unitCalculation.NearbyAllies.Where(e => e.Unit.BuildProgress == 1 && (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN)).Concat(new List<UnitCalculation> { unitCalculation });
            var enemies = unitCalculation.NearbyEnemies.Where(e => e.Unit.BuildProgress == 1 && (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN));

            var calculation = CalculateTargetPriority(allies, enemies);

            if (ShouldTargetDetection(unitCalculation))
            {
                calculation.TargetPriority = TargetPriority.KillDetection;
                return calculation;
            }

            calculation.FrameCalculated = frame;
            return calculation;
        }

        public TargetPriorityCalculation CalculateTargetPriority(IEnumerable<UnitCalculation> allies, IEnumerable<UnitCalculation> enemies)
        {
            var calculation = new TargetPriorityCalculation
            {
                TargetPriority = TargetPriority.Attack
            };

            allies = allies.Where(a => a.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR);
            enemies = enemies.Where(a => a.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR);

            var allyHealth = allies.Sum(e => e.SimulatedHitpoints);
            var enemyHealth = enemies.Sum(e => e.SimulatedHitpoints);

            var allyAttributes = allies.SelectMany(e => e.Attributes).Distinct();
            var enemyAttributes = enemies.SelectMany(e => e.Attributes).Distinct();

            var allyDps = allies.Sum(e => e.SimulatedDamagePerSecond(enemyAttributes, true, true));
            var enemyDps = enemies.Sum(e => e.SimulatedDamagePerSecond(allyAttributes, true, true));

            var rangedAllies = allies.Where(e => e.Range > 2);
            if (rangedAllies.Count() > 0)
            {
                var fastestEnemy = enemies.OrderByDescending(e => e.UnitTypeData.MovementSpeed).FirstOrDefault();
                if (fastestEnemy == null || rangedAllies.Any(a => a.UnitTypeData.MovementSpeed >= fastestEnemy.UnitTypeData.MovementSpeed))
                {
                    enemyDps = enemyDps / 2f;
                }
            }

            var allyHps = allies.Sum(e => e.SimulatedHealPerSecond);
            var enemyHps = enemies.Sum(e => e.SimulatedHealPerSecond);

            var secondsToKillEnemies = 1000f;
            if (allyDps - enemyHps > 0)
            {
                secondsToKillEnemies = enemyHealth / (allyDps - enemyHps);
            }

            var secondsToKillAllies = 1000f;
            if (enemyDps - allyHps > 0)
            {
                secondsToKillAllies = allyHealth / (enemyDps - allyHps);
            }

            calculation.OverallWinnability = secondsToKillAllies / secondsToKillEnemies; // higher the number the better

            if (secondsToKillEnemies == 0)
            {
                calculation.OverallWinnability = 1000f;
            }
            calculation.Overwhelm = calculation.OverallWinnability > 20;

            var airAttackingEnemies = enemies.Where(e => e.DamageAir || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY);
            var airKillSeconds = 1000f;
            if (allies.Count(a => a.Unit.IsFlying && a.DamageAir) > 0)
            {
                var seconds = GetKillSeconds(airAttackingEnemies, allies);
                if (seconds > 0)
                {
                    airKillSeconds = seconds;
                }
                else
                {
                    airKillSeconds = 0.00001f;
                }
            }

            calculation.AirWinnability = secondsToKillAllies / airKillSeconds;
            if (!enemies.Any(e => e.DamageAir) && allies.Any(a => a.Unit.IsFlying))
            {
                calculation.AirWinnability = 1000f;
            }

            var groundAttackingEnemies = enemies.Where(e => e.DamageGround || e.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY);
            var groundKillSeconds = 1000f;
            if (allies.Count(a => !a.Unit.IsFlying && a.DamageGround) > 0)
            {
                var seconds = GetKillSeconds(groundAttackingEnemies, allies);
                if (seconds > 0)
                {
                    groundKillSeconds = seconds;
                }
                else
                {
                    groundKillSeconds = 0.00001f;
                }
            }

            calculation.GroundWinnability = secondsToKillAllies / groundKillSeconds;
            if (!enemies.Any(e => e.DamageGround) && allies.Any(a => !a.Unit.IsFlying))
            {
                calculation.GroundWinnability = 1000f;
            }

            if (allies.All(a => a.Unit.IsFlying))
            {
                calculation.OverallWinnability = calculation.AirWinnability;
            }
            else if (allies.All(a => !a.Unit.IsFlying))
            {
                calculation.OverallWinnability = calculation.GroundWinnability;
            }

            if (calculation.OverallWinnability < 1 && calculation.AirWinnability < 1 && calculation.GroundWinnability < 1)
            {
                if (enemyHps > enemyDps && groundAttackingEnemies.Count(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV) > 2)
                {
                    var lowerHps = enemyHealth / (allyDps - (enemyHps / 5));
                    if (secondsToKillAllies / lowerHps > 1)
                    {
                        calculation.TargetPriority = TargetPriority.KillWorkers; // can win by targetting repairing scvs
                        return calculation;
                    }
                }

                calculation.TargetPriority = TargetPriority.Retreat;
                return calculation;
            }

            if (enemies.Sum(e => e.Repairers) >= 5)
            {
                calculation.TargetPriority = TargetPriority.KillWorkers;
                return calculation;
            }

            if (calculation.GroundWinnability > calculation.AirWinnability)
            {
                var bunker = groundAttackingEnemies.Where(b => b.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER).FirstOrDefault();
                if (bunker != null && groundAttackingEnemies.Count() < 10)
                {
                    if (bunker.Unit.BuildProgress < 1 || (bunker.Unit.Health > 100 && enemyHps > enemyDps && bunker.Repairers > 2))
                    {
                        calculation.TargetPriority = TargetPriority.KillWorkers;
                        return calculation;
                    }

                    calculation.TargetPriority = TargetPriority.KillBunker;
                    return calculation;
                }
                calculation.TargetPriority = TargetPriority.WinGround;
            }
            else if (calculation.AirWinnability > calculation.GroundWinnability)
            {
                calculation.TargetPriority = TargetPriority.WinAir;
            }

            return calculation;
        }

        bool ShouldTargetDetection(UnitCalculation unitCalculation)
        {
            if (unitCalculation.NearbyAllies.Any(e => SharkyUnitData.CloakableAttackers.Contains((UnitTypes)e.Unit.UnitType)))
            {
                if (unitCalculation.NearbyEnemies.Any(e => SharkyUnitData.DetectionTypes.Contains((UnitTypes)e.Unit.UnitType) || SharkyUnitData.AbilityDetectionTypes.Contains((UnitTypes)e.Unit.UnitType)))
                {
                    return true;
                }
            }
            return false;
        }

        float GetKillSeconds(IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> allies)
        {
            var groundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();

            var flyingGroundAttackingEnemies = enemies.Where(e => e.Unit.IsFlying);
            var flyingGroundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();
            var airHps = flyingGroundAttackingEnemies.Sum(e => e.SimulatedHealPerSecond);
            var airHealth = flyingGroundAttackingEnemies.Sum(e => e.SimulatedHitpoints);

            var groundedGroundAttackingEnemies = enemies.Where(e => !e.Unit.IsFlying);
            var groundedGroundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();
            var groundHps = groundedGroundAttackingEnemies.Sum(e => e.SimulatedHealPerSecond);
            var groundHealth = groundedGroundAttackingEnemies.Sum(e => e.SimulatedHitpoints);

            var groundAttackingAllies = allies.Where(e => e.DamageGround || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || e.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC);
            var liftsAvailable = allies.Count(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX && a.Unit.Energy >= 50) > 1;
            if (liftsAvailable)
            {
                groundAttackingAllies = allies;
            }

            var airAttackingAllies = allies.Where(e => e.DamageAir);
            var bothAttackingAllies = allies.Where(e => e.DamageAir || e.DamageGround);
            var groundAttackDps = groundAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(groundedGroundAttackingEnemiesAttributes, false, true));
            var airAttackDps = airAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(flyingGroundAttackingEnemiesAttributes, true, false));
            var bothAttackingDps = bothAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(groundAttackingEnemiesAttributes, true, true));
            if (liftsAvailable)
            {
                groundAttackDps = bothAttackingDps;
            }

            var secondsToKillAirGroundAttackingEnemies = 600f;
            if (airAttackDps - airHps > 0)
            {
                if (airHealth == 0)
                {
                    secondsToKillAirGroundAttackingEnemies = 0;
                }
                else
                {
                    secondsToKillAirGroundAttackingEnemies = airHealth / (airAttackDps - airHps);
                }
            }
            var secondsToKillGroundGroundAttackingEnemies = 600f;
            if (groundAttackDps - groundHps > 0)
            {
                if (groundHealth == 0)
                {
                    secondsToKillGroundGroundAttackingEnemies = 0;
                }
                else
                {
                    secondsToKillGroundGroundAttackingEnemies = groundHealth / (groundAttackDps - groundHps);
                }
            }

            var secondsBothToKillAirGroundAttackingEnemies = 600f;
            if (bothAttackingDps - airHps > 0)
            {
                if (airHealth == 0)
                {
                    secondsBothToKillAirGroundAttackingEnemies = 0;
                }
                else
                {
                    secondsBothToKillAirGroundAttackingEnemies = airHealth / (bothAttackingDps - airHps);
                }
            }
            var secondsBothToKillGroundGroundAttackingEnemies = 600f;
            if (bothAttackingDps - groundHps > 0)
            {
                if (groundHealth == 0)
                {
                    secondsBothToKillGroundGroundAttackingEnemies = 0;
                }
                else
                {
                    secondsBothToKillGroundGroundAttackingEnemies = groundHealth / (bothAttackingDps - groundHps);
                }
            }

            var airSeconds = secondsToKillAirGroundAttackingEnemies - secondsBothToKillGroundGroundAttackingEnemies;
            var groundSeconds = secondsToKillGroundGroundAttackingEnemies - secondsBothToKillAirGroundAttackingEnemies;

            var groundKillSeconds = airSeconds;
            if (groundSeconds > airSeconds || airHealth == 0)
            {
                groundKillSeconds = groundSeconds;
            }

            return groundKillSeconds;
        }

        public TargetPriorityCalculation CalculateGeneralTargetPriority()
        {
            var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && Vector2.DistanceSquared(attackVector, e.Position) < 625)); // every enemy unit no matter where it is, defensive structures within 25 range of attack point
            var friendlyUnits = ActiveUnitData.SelfUnits.Values.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit));
            return CalculateTargetPriority(friendlyUnits, enemyUnits);
        }
    }
}
