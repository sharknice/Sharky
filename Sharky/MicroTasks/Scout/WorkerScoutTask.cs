namespace Sharky.MicroTasks
{
    public class WorkerScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        BaseData BaseData;
        AreaService AreaService;
        MineralWalker MineralWalker;
        MicroData MicroData;

        List<Point2D> ScoutPoints;

        public bool ScoutOnlyMain { get; set; }

        bool started { get; set; }

        public WorkerScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
            AreaService = defaultSharkyBot.AreaService;
            MineralWalker = defaultSharkyBot.MineralWalker;
            MicroData = defaultSharkyBot.MicroData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            ScoutOnlyMain = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var commands = new List<SC2Action>();
            if (TargetingData.EnemyMainBasePoint == null) { return commands; }

            UpdateScoutPoints();

            var mainVector = new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y);
            var points = ScoutPoints.OrderBy(p => MapDataService.LastFrameVisibility(p)).ThenBy(p => Vector2.DistanceSquared(mainVector, new Vector2(p.X, p.Y)));

            foreach (var commander in UnitCommanders)
            {
                if (!ScoutOnlyMain && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT))
                {
                    ScoutOnlyMain = true;
                    ScoutPoints = null;
                    UpdateScoutPoints();
                }

                if (commander.UnitCalculation.Unit.ShieldMax > 5 && (commander.UnitCalculation.Unit.Shield < 5 || (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.EnemiesInRangeOf.Any())))
                {
                    if (MineralWalker.MineralWalkHome(commander, frame, out List<SC2Action> mineralWalk))
                    {
                        commands.AddRange(mineralWalk);
                        continue;
                    }
                }

                if (commander.UnitCalculation.NearbyEnemies.Count() > 1 && (commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.ShieldMax + commander.UnitCalculation.Unit.HealthMax) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
                {
                    var enemy = GetEnemyBuildingScv(commander.UnitCalculation.NearbyEnemies);
                    if (enemy != null)
                    {
                        var attackAction = commander.Order(frame, Abilities.ATTACK, targetTag: enemy.Unit.Tag);
                        if (attackAction != null)
                        {
                            commands.AddRange(attackAction);
                        }
                        continue;
                    }
                }

                if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any(e => e.Range > 1 || e.Damage > 5))
                {
                    var retreatAction = MicroData.IndividualMicroControllers[(UnitTypes)commander.UnitCalculation.Unit.UnitType].Retreat(commander, points.FirstOrDefault(), null, frame);
                    if (retreatAction != null)
                    {
                        commands.AddRange(retreatAction);
                    }
                    continue;
                }

                var action = commander.Order(frame, Abilities.MOVE, points.FirstOrDefault());
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            return commands;
        }

        private void UpdateScoutPoints()
        {
            if (ScoutPoints == null)
            {
                ScoutPoints = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);
                if (ScoutOnlyMain)
                {
                    GetPathAroundMainPerimiter();
                }
                else 
                {
                    ScoutPoints = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);
                    ScoutPoints.Add(BaseData.EnemyBaseLocations.Skip(1).First().Location);
                    var ramp = TargetingData.ChokePoints.Bad.FirstOrDefault();
                    if (ramp != null)
                    {
                        ScoutPoints.Add(new Point2D { X = ramp.Center.X, Y = ramp.Center.Y });
                    }
                }
            }
        }

        private void GetPathAroundMainPerimiter()
        {
            ScoutPoints = ScoutPoints.Where(p => !MapDataService.PathWalkable(p, 3) && MapDataService.PathWalkable(p, 2)).ToList();
        }

        UnitCalculation GetEnemyBuildingScv(List<UnitCalculation> enemies)
        {
            var unfinishedBuilding = enemies.FirstOrDefault(e => e.Unit.BuildProgress < 1);
            if (unfinishedBuilding != null)
            {
                var scv = enemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV).OrderBy(e => Vector2.DistanceSquared(e.Position, unfinishedBuilding.Position)).FirstOrDefault();
                if (scv != null)
                {
                    return scv;
                }
            }
            return null;
        }
    }
}
