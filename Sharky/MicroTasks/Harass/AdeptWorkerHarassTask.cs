namespace Sharky.MicroTasks
{
    public class AdeptWorkerHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        IIndividualMicroController AdeptMicroController;
        public IIndividualMicroController AdeptShadeMicroController { get; set; }

        Point2D EnemyMain { get; set; }
        Point2D EnemyExpansion { get; set; }

        public int MaxAdeptCount { get; set; }

        public bool PrioritizeExpansion { get; set; }

        public AdeptWorkerHarassTask(BaseData baseData, TargetingData targetingData, IIndividualMicroController adeptMicroController, IIndividualMicroController adeptShadeMicroController, bool enabled = false, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            AdeptMicroController = adeptMicroController;
            AdeptShadeMicroController = adeptShadeMicroController;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            MaxAdeptCount = 100;
            PrioritizeExpansion = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT))
                {
                    if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT && UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT) < MaxAdeptCount)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    else if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT)
                    {
                        if (commander.Value.ParentUnitCalculation != null && UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == commander.Value.ParentUnitCalculation.Unit.Tag))
                        {
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            SetBases();

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)
                {
                    if (commander.UnitCalculation.Unit.WeaponCooldown == 0)
                    {
                        var workerInRange = commander.UnitCalculation.EnemiesInRange.Where(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)).OrderBy(e => e.Unit.Health).FirstOrDefault();
                        if (workerInRange != null)
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: workerInRange.Unit.Tag));
                            continue;
                        }
                        var otherInRange = commander.UnitCalculation.EnemiesInRange.Where(e => e.Damage > 0).OrderBy(e => e.Unit.Health).FirstOrDefault();
                        if (otherInRange != null)
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: otherInRange.Unit.Tag));
                            continue;
                        }
                    }

                    if (PrioritizeExpansion && Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(EnemyMain.X, EnemyMain.Y)) < 225 && commander.UnitCalculation.EnemiesThreateningDamage.Any())
                    {
                        var action = commander.Order(frame, Abilities.MOVE, TargetingData.MainDefensePoint);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }

                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(EnemyMain.X, EnemyMain.Y)) < 100)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
                        {
                            var action = AdeptMicroController.HarassWorkers(commander, EnemyMain, EnemyExpansion, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                        else
                        {
                            var action = AdeptMicroController.NavigateToPoint(commander, EnemyExpansion, TargetingData.ForwardDefensePoint, null, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(EnemyExpansion.X, EnemyExpansion.Y)) < 100 && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
                    {
                        var action = AdeptMicroController.HarassWorkers(commander, EnemyExpansion, EnemyMain, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var target = EnemyMain;
                        if (PrioritizeExpansion)
                        {
                            target = BaseData.EnemyBaseLocations.Skip(3).FirstOrDefault().Location;
                            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) < 100)
                            {
                                target = EnemyExpansion;
                            }
                        }
                        var action = AdeptMicroController.NavigateToPoint(commander, target, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
                else
                {
                    var target = EnemyMain;
                    if (commander.ParentUnitCalculation != null)
                    {
                        if (Vector2.DistanceSquared(commander.ParentUnitCalculation.Position, new Vector2(EnemyMain.X, EnemyMain.Y)) < 100)
                        {
                            target = EnemyExpansion;
                        }
                    }

                    if (PrioritizeExpansion)
                    {
                        target = EnemyExpansion;                     
                    }

                    var action = AdeptShadeMicroController.NavigateToPoint(commander, target, target, target, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }

        private void SetBases()
        {
            if (EnemyMain == null)
            {
                var mainBase = BaseData.EnemyBaseLocations.FirstOrDefault();
                if (mainBase != null)
                {
                    EnemyMain = mainBase.MineralLineBuildingLocation;
                }
                var expansionBase = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                if (expansionBase != null)
                {
                    EnemyExpansion = expansionBase.MineralLineBuildingLocation;
                }
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                Deaths += UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT);
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
