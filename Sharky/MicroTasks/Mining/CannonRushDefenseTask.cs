namespace Sharky.MicroTasks.Mining
{
    public class CannonRushDefenseTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;
        RequirementService RequirementService;

        int LastClaimFrame;

        public CannonRushDefenseTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            TargetingData = defaultSharkyBot.TargetingData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            RequirementService = defaultSharkyBot.RequirementService;

           Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            LastClaimFrame = 0;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg || EnemyData.EnemyRace == SC2APIProtocol.Race.Terran || TargetingData.SelfMainBasePoint == null)
            {
                Disable();
                return new List<SC2APIProtocol.Action>();
            }

            if (EnemyData.SelfRace == Race.Protoss && !UnitCommanders.Any() && RequirementService.HaveCompleted(UnitTypes.PROTOSS_CYBERNETICSCORE))
            {
                Disable();
                return new List<SC2APIProtocol.Action>();
            }

            var commands = new List<SC2APIProtocol.Action>();

            var enemyCannons = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && !e.NearbyAllies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS)).OrderBy(c => Vector2.DistanceSquared(new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y), c.Position)).Where(c => Vector2.DistanceSquared(new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y), c.Position) < 2500);
            var enemyPylons = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && !e.NearbyAllies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS) && !e.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit))).OrderBy(c => Vector2.DistanceSquared(new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y), c.Position)).Where(c => Vector2.DistanceSquared(new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y), c.Position) < 2500);

            if (!enemyCannons.Any(e => e.Unit.BuildProgress == 1 && e.Unit.Shield > 5) && (enemyPylons.Any() || enemyCannons.Any(e => e.Unit.BuildProgress < 1 || e.Unit.Shield <= 5)))
            {
                foreach (var enemyCannon in enemyCannons)
                {
                    if (enemyCannon.Unit.Shield < 5 || enemyCannon.Unit.BuildProgress < 1)
                    {
                        commands.AddRange(DefendAgainstCannon(enemyCannon, frame));
                    }
                }
                foreach (var enemyPylon in enemyPylons)
                {
                    commands.AddRange(DefendAgainstCannon(enemyPylon, frame));
                }
                foreach (var commander in UnitCommanders.Where(c => !c.UnitCalculation.Unit.Orders.Any() && c.LastOrderFrame < frame - 3))
                {
                    var enemy = enemyCannons.FirstOrDefault();
                    if (enemy == null)
                    {
                        enemy = enemyPylons.FirstOrDefault();
                    }
                    commands.AddRange(commander.Order(frame, Abilities.ATTACK, new SC2APIProtocol.Point2D { X = enemy.Position.X, Y = enemy.Position.Y }));
                }
            }
            else
            {
                foreach (var commander in UnitCommanders)
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                }
                UnitCommanders.Clear();
            }

            return commands;
        }

        private IEnumerable<SC2APIProtocol.Action> DefendAgainstCannon(UnitCalculation enemyCannon, int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            var commandersAttacking = UnitCommanders.Where(c => c.UnitCalculation.Unit.Orders.Any(o => o.TargetUnitTag == enemyCannon.Unit.Tag));
            if (commandersAttacking.Count() < 4)
            {
                var unUsedCommanders = UnitCommanders.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK));
                foreach (var commander in unUsedCommanders)
                {
                    var action = commander.Order(frame, Abilities.ATTACK, targetTag: enemyCannon.Unit.Tag, allowSpam: true);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                var needed = 4 - unUsedCommanders.Count();

                if (needed > 0)
                {
                    ClaimDefender(enemyCannon, frame);
                }
            }

            return commands;
        }

        private void ClaimDefender(UnitCalculation enemyCannon, int frame)
        {
            if (LastClaimFrame > frame - 3 || UnitCommanders.Count() > 9) { return; }

            var worker = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && 
                (c.UnitRole == UnitRole.Minerals || c.UnitRole == UnitRole.Gas) && 
                c.UnitCalculation.Unit.Health + c.UnitCalculation.Unit.Shield >= 40).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, enemyCannon.Position)).FirstOrDefault();

            if (worker != null)
            {
                MicroTaskData[typeof(MiningTask).Name].StealUnit(worker);
                worker.UnitRole = UnitRole.Defend;
                worker.Claimed = true;
                UnitCommanders.Add(worker);
                LastClaimFrame = frame;
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            base.RemoveDeadUnits(deadUnits);
        }
    }
}
