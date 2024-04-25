namespace Sharky.MicroTasks
{
    public class DefenseService
    {
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        TargetPriorityService TargetPriorityService;

        public HashSet<UnitTypes> UnSplittableUnitTypes { get; set; }

        public DefenseService(DefaultSharkyBot defaultSharkyBot, TargetPriorityService targetPriorityService)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            TargetPriorityService = targetPriorityService;

            UnSplittableUnitTypes = new HashSet<UnitTypes>();
        }

        public List<UnitCommander> GetDefenseGroup(List<UnitCalculation> enemyGroup, List<UnitCommander> unitCommanders, bool defendToDeath)
        {
            var position = enemyGroup.FirstOrDefault().Unit.Pos;
            var enemyGroupLocation = new Vector2(position.X, position.Y);

            var enemyHealth = enemyGroup.Sum(e => e.SimulatedHitpoints);
            var enemyDps = enemyGroup.Sum(e => e.SimulatedDamagePerSecond(new List<SC2Attribute>(), true, true));
            var enemyHps = enemyGroup.Sum(e => e.SimulatedHealPerSecond);
            var enemyAttributes = enemyGroup.SelectMany(e => e.Attributes).Distinct();
            var hasGround = enemyGroup.Any(e => !e.Unit.IsFlying);
            var hasAir = enemyGroup.Any(e => e.Unit.IsFlying);
            var cloakable = enemyGroup.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Cloakable));

            var counterGroup = new List<UnitCommander>();

            foreach (var commander in unitCommanders.Where(c => defendToDeath || CanSplitCommander(c)))
            {
                if ((hasGround && commander.UnitCalculation.DamageGround) || (hasAir && commander.UnitCalculation.DamageAir) || (cloakable && (commander.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Detector) || commander.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.DetectionCaster))) || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX)
                {
                    counterGroup.Add(commander);

                    var targetPriority = TargetPriorityService.CalculateTargetPriority(counterGroup.Select(c => c.UnitCalculation), enemyGroup);
                    if (targetPriority.Overwhelm || (targetPriority.AirWinnability > 1 && targetPriority.GroundWinnability > 1))
                    {
                        return counterGroup;
                    }
                }
            }

            var finalTargetPriority = TargetPriorityService.CalculateTargetPriority(counterGroup.Select(c => c.UnitCalculation), enemyGroup);
            if (finalTargetPriority.OverallWinnability < 1)
            {
                var unsplittables = unitCommanders.Where(c => !CanSplitCommander(c));
                foreach (var unsplittable in unsplittables)
                {
                    counterGroup.Add(unsplittable);
                }
                if (unsplittables.Any())
                {
                    finalTargetPriority = TargetPriorityService.CalculateTargetPriority(counterGroup.Select(c => c.UnitCalculation), enemyGroup);
                }
            }

            if (finalTargetPriority.OverallWinnability > .5)
            {
                return counterGroup;
            }
            if (EnemyData.SelfRace == Race.Protoss && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && u.Value.Unit.IsPowered && u.Value.Unit.BuildProgress >= 1 && (u.Value.Unit.Energy > 3 || u.Value.Unit.BuffIds.Contains((uint)Buffs.BATTERYOVERCHARGE)) && Vector2.DistanceSquared(enemyGroupLocation, u.Value.Position) < 64))
            {
                // always defend by shield batteries
                return counterGroup;
            }

            if (defendToDeath)
            {
                return counterGroup;
            }

            return new List<UnitCommander>();
        }

        public List<List<UnitCalculation>> GetEnemyGroups(IEnumerable<UnitCalculation> enemies)
        {
            var enemyGroups = new List<List<UnitCalculation>>();
            foreach (var enemy in enemies)
            {
                if (!enemyGroups.Any(g => g.Any(e => e.Unit.Tag == enemy.Unit.Tag)))
                {
                    if (ActiveUnitData.EnemyUnits.ContainsKey(enemy.Unit.Tag))
                    {
                        var group = new List<UnitCalculation>();
                        group.Add(enemy);
                        foreach (var nearbyEnemy in ActiveUnitData.EnemyUnits[enemy.Unit.Tag].NearbyAllies)
                        {
                            if (!enemyGroups.Any(g => g.Any(e => e.Unit.Tag == nearbyEnemy.Unit.Tag)))
                            {
                                group.Add(nearbyEnemy);
                            }
                        }
                        enemyGroups.Add(group);
                    }
                }
            }
            return enemyGroups;
        }

        public IEnumerable<UnitCommander> OverwhelmSplit(ArmySplits split, List<UnitCommander> availableCommanders)
        {
            var reinforcements = new List<UnitCommander>();
            var targetPriority = TargetPriorityService.CalculateTargetPriority(split.SelfGroup.Select(c => c.UnitCalculation), split.EnemyGroup);
            if (targetPriority.Overwhelm) { return reinforcements; }

            var enemyHealth = split.EnemyGroup.Sum(e => e.SimulatedHitpoints);
            var enemyDps = split.EnemyGroup.Sum(e => e.SimulatedDamagePerSecond(new List<SC2Attribute>(), true, true));
            var enemyHps = split.EnemyGroup.Sum(e => e.SimulatedHealPerSecond);
            var enemyAttributes = split.EnemyGroup.SelectMany(e => e.Attributes).Distinct();
            var hasGround = split.EnemyGroup.Any(e => !e.Unit.IsFlying);
            var hasAir = split.EnemyGroup.Any(e => e.Unit.IsFlying);
            var cloakable = split.EnemyGroup.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Cloakable));

            var counterGroup = new List<UnitCommander>();
            counterGroup.AddRange(split.SelfGroup);

            foreach (var commander in availableCommanders.Where(c => CanSplitCommander(c)))
            {
                if ((hasGround && commander.UnitCalculation.DamageGround) || (hasAir && commander.UnitCalculation.DamageAir) || (cloakable && (commander.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Detector) || commander.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.DetectionCaster))) || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX)
                {
                    reinforcements.Add(commander);
                    counterGroup.Add(commander);

                    targetPriority = TargetPriorityService.CalculateTargetPriority(counterGroup.Select(c => c.UnitCalculation), split.EnemyGroup);
                    if (targetPriority.Overwhelm)
                    {
                        return reinforcements;
                    }
                }
            }

            var unsplittables = availableCommanders.Where(c => !CanSplitCommander(c));
            foreach (var unsplittable in unsplittables)
            {
                counterGroup.Add(unsplittable);
            }

            return reinforcements;
        }

        bool CanSplitCommander(UnitCommander commander)
        {
            return !UnSplittableUnitTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType);
        }
    }
}
