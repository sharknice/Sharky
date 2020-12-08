using SC2APIProtocol;
using Sharky.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class DefenseService
    {
        IUnitManager UnitManager;

        public DefenseService(IUnitManager unitManager)
        {
            UnitManager = unitManager;
        }

        public List<UnitCommander> GetDefenseGroup(List<UnitCalculation> enemyGroup, List<UnitCommander> unitCommanders)
        {
            var position = enemyGroup.FirstOrDefault().Unit.Pos;
            var enemyGroupLocation = new Vector2(position.X, position.Y);

            var enemyHealth = enemyGroup.Sum(e => e.SimulatedHitpoints);
            var enemyDps = enemyGroup.Sum(e => e.SimulatedDamagePerSecond(new List<Attribute>(), true, true));
            var enemyHps = enemyGroup.Sum(e => e.SimulatedHealPerSecond);
            var enemyAttributes = enemyGroup.SelectMany(e => e.Attributes).Distinct();
            var hasGround = enemyGroup.All(e => e.Unit.IsFlying);
            var hasAir = enemyGroup.All(e => e.Unit.IsFlying);
            var cloakable = enemyGroup.Any(e => e.UnitClassifications.Contains(UnitClassification.Cloakable));

            var counterGroup = new List<UnitCommander>();

            foreach (var commander in unitCommanders)
            {
                if ((hasGround && commander.UnitCalculation.DamageGround) || (hasAir && commander.UnitCalculation.DamageAir) || (cloakable && (commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) || commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster))))
                {
                    counterGroup.Add(commander);

                    var wwinnability = CalculateWinability(counterGroup, enemyAttributes, enemyHps, enemyHealth, enemyDps);
                    if (wwinnability > 2)
                    {
                        return counterGroup;
                    }
                }
            }

            return counterGroup;
        }

        float CalculateWinability(List<UnitCommander> counterGroup, IEnumerable<Attribute> enemyAttributes, float enemyHps, float enemyHealth, float enemyDps)
        {
            var allyHealth = counterGroup.Sum(c => c.UnitCalculation.SimulatedHitpoints);
            var allyDps = counterGroup.Sum(c => c.UnitCalculation.SimulatedDamagePerSecond(enemyAttributes, true, true));
            var allyHps = counterGroup.Sum(c => c.UnitCalculation.SimulatedHealPerSecond);

            var secondsToKillEnemies = 600f;
            if (allyDps - enemyHps > 0)
            {
                secondsToKillEnemies = enemyHealth / (allyDps - enemyHps);
            }

            var secondsToKillAllies = 600f;
            if (enemyDps - allyHps > 0)
            {
                secondsToKillAllies = allyHealth / (enemyDps - allyHps);
            }

            return secondsToKillAllies / secondsToKillEnemies;
        }

        public List<List<UnitCalculation>> GetEnemyGroups(IEnumerable<UnitCalculation> enemies)
        {
            var enemyGroups = new List<List<UnitCalculation>>();
            foreach (var enemy in enemies)
            {
                if (!enemyGroups.Any(g => g.Any(e => e.Unit.Tag == enemy.Unit.Tag)))
                {
                    var group = new List<UnitCalculation>();
                    group.Add(enemy);
                    foreach (var nearbyEnemy in UnitManager.EnemyUnits[enemy.Unit.Tag].NearbyAllies)
                    {
                        if (!enemyGroups.Any(g => g.Any(e => e.Unit.Tag == nearbyEnemy.Unit.Tag)))
                        {
                            group.Add(nearbyEnemy);
                        }
                    }
                    enemyGroups.Add(group);
                }
            }
            return enemyGroups;
        }
    }
}
