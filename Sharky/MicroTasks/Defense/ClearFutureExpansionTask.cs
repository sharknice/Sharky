namespace Sharky.MicroTasks
{
    public class ClearFutureExpansionTask : MicroTask
    {
        TargetingData TargetingData;
        BaseData BaseData;
        MacroData MacroData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;
        ActiveUnitData ActiveUnitData;
        BuildingService BuildingService;
        MapDataService MapDataService;

        IMicroController MicroController;

        Point2D NextBaseLocation;
        int BaseCountDuringLocation;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }
        
        /// <summary>
        /// only uses units when the desired bases is less than the current, claims them from the attack class then unclaims them when done
        /// will take workers and mine minerals blocking the expansion
        /// </summary>
        public bool OnlyActiveWhenNeeded { get; set; }
        bool Needed;
        List<UnitCalculation> BlockingMinerals;

        public ClearFutureExpansionTask(DefaultSharkyBot defaultSharkyBot,
            List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            MacroData = defaultSharkyBot.MacroData;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BuildingService = defaultSharkyBot.BuildingService;
            MapDataService = defaultSharkyBot.MapDataService;

            MicroController = defaultSharkyBot.MicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Enabled = true;

            OnlyActiveWhenNeeded = false;
            Needed = false;
            BlockingMinerals = new List<UnitCalculation>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (OnlyActiveWhenNeeded && !Needed) { return; }

            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            UpdateNeeded();

            if (UpdateBaseLocation())
            {
                if (OnlyActiveWhenNeeded && !Needed)
                {
                    MineOutBlockingMinerals(frame, actions);
                    return actions;
                }

                var detectors = UnitCommanders.Where(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Detector) || c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.DetectionCaster));
                var nonDetectors = UnitCommanders.Where(c => !c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Detector) && !c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.DetectionCaster) && !c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker));

                var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);

                foreach (var nonDetector in nonDetectors)
                {
                    if (nonDetector.UnitCalculation.Unit.ShieldMax > 0 && nonDetector.UnitCalculation.Unit.Shield == 0)
                    {
                        actions.AddRange(MicroController.Retreat(new List<UnitCommander> { nonDetector }, TargetingData.ForwardDefensePoint, null, frame));
                        continue;
                    }
                    var creepTumor = nonDetector.UnitCalculation.NearbyEnemies.Where(e => (e.Unit.UnitType == (uint)UnitTypes.ZERG_CREEPTUMORBURROWED || (uint)UnitTypes.ZERG_CREEPTUMOR == e.Unit.UnitType) && e.Unit.DisplayType == DisplayType.Visible).OrderBy(e => Vector2.Distance(e.Position, vector)).FirstOrDefault();
                    if (!nonDetector.UnitCalculation.EnemiesInRangeOfAvoid.Any() && creepTumor != null)
                    {
                        actions.AddRange(nonDetector.Order(frame, Abilities.ATTACK, creepTumor.Position.ToPoint2D()));
                        continue;
                    }
                    else if (nonDetector.UnitCalculation.EnemiesThreateningDamage.Any() || Vector2.DistanceSquared(nonDetector.UnitCalculation.Position, vector) < 33)
                    {
                        actions.AddRange(MicroController.Attack(new List<UnitCommander> { nonDetector }, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        actions.AddRange(nonDetector.Order(frame, Abilities.MOVE, NextBaseLocation));
                    }
                }

                foreach (var detector in detectors)
                {
                    if (detector.UnitCalculation.EnemiesThreateningDamage.Any())
                    {
                        actions.AddRange(MicroController.Support(new List<UnitCommander> { detector }, nonDetectors, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        if (detector.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.DetectionCaster))
                        {
                            if (detector.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE)
                            {
                                if (detector.UnitCalculation.Unit.Energy > 25 && !detector.UnitCalculation.NearbyEnemies.Any(e => e.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)))
                                {
                                    actions.AddRange(detector.Order(frame, Abilities.EFFECT_ORACLEREVELATION, NextBaseLocation));
                                    continue;
                                }
                                if (detector.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON))
                                {
                                    actions.AddRange(detector.Order(frame, Abilities.ATTACK, NextBaseLocation));
                                    continue;
                                }
                                else if (detector.UnitCalculation.EnemiesInRange.Any())
                                {
                                    actions.AddRange(detector.Order(frame, Abilities.BEHAVIOR_PULSARBEAMON));
                                    continue;
                                }
                            }
                        }

                        if (Vector2.Distance(detector.UnitCalculation.Position, vector) < 7 && !detector.UnitCalculation.EnemiesInRangeOfAvoid.Any())
                        {
                            var creepTumor = detector.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_CREEPTUMORBURROWED || (uint)UnitTypes.ZERG_CREEPTUMOR == e.Unit.UnitType).OrderBy(e => Vector2.Distance(e.Position, vector)).FirstOrDefault();
                            if (creepTumor != null)
                            {
                                actions.AddRange(detector.Order(frame, Abilities.MOVE, creepTumor.Position.ToPoint2D()));
                                continue;
                            }
                            var cell = MapDataService.GetCells(NextBaseLocation.X, NextBaseLocation.Y, 5).Where(c => MapDataService.IsOnCreep(new Point2D { X = c.X, Y = c.Y })).OrderByDescending(c => Vector2.Distance(new Vector2(c.X, c.Y), vector)).FirstOrDefault();
                            if (cell != null)
                            {
                                actions.AddRange(detector.Order(frame, Abilities.MOVE, new Point2D { X = cell.X, Y = cell.Y }));
                                continue;
                            }
                        }

                        if (Vector2.DistanceSquared(detector.UnitCalculation.Position, vector) < 4)
                        {
                            actions.AddRange(MicroController.Support(new List<UnitCommander> { detector }, nonDetectors, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                        }
                        else
                        {
                            actions.AddRange(detector.Order(frame, Abilities.MOVE, NextBaseLocation));
                        }
                    }
                }

                MineOutBlockingMinerals(frame, actions);
            }
            else
            {
                actions.AddRange(MicroController.Attack(UnitCommanders, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, null, frame));
            }

            return actions;
        }

        private void MineOutBlockingMinerals(int frame, List<SC2APIProtocol.Action> actions)
        {
            var workers = UnitCommanders.Where(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker));
            if (BlockingMinerals.Any())
            {
                var mineral = BlockingMinerals.FirstOrDefault();
                var realMineral = ActiveUnitData.NeutralUnits.Values.FirstOrDefault(u => u.Position.X == mineral.Position.X && u.Position.Y == mineral.Position.Y);
                if (realMineral != null)
                {
                    foreach (var worker in workers)
                    {
                        if (worker.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.CARRYMINERALFIELDMINERALS))
                        {
                            actions.AddRange(worker.Order(frame, Abilities.HARVEST_RETURN));
                        }
                        else
                        {
                            actions.AddRange(worker.Order(frame, Abilities.HARVEST_GATHER, null, realMineral.Unit.Tag));
                        }
                    }
                }
                else
                {
                    BlockingMinerals.Remove(mineral);
                }
            }
            else
            {
                foreach (var commander in UnitCommanders.Where(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker)))
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                }
                UnitCommanders.RemoveAll(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker));
            }
        }

        private void UpdateNeeded()
        {
            if (!OnlyActiveWhenNeeded) { return; }

            if ((EnemyData.SelfRace == Race.Zerg && MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] > BaseData.SelfBases.Count()) ||
                (EnemyData.SelfRace == Race.Terran && MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] > BaseData.SelfBases.Count()) ||
                 (EnemyData.SelfRace == Race.Protoss && MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] > BaseData.SelfBases.Count()))
            {
                if (!Needed)
                {
                    StealFromAttackTask();
                }
                Needed = true;
            }
            else
            {
                Needed = false;
                foreach (var commander in UnitCommanders.Where(u => !u.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker)))
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                }
                UnitCommanders.RemoveAll(u => !u.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker));
            }
        }

        void StealFromAttackTask()
        {
            if (NextBaseLocation == null) { return; }
            var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);
            if (MicroTaskData.ContainsKey(typeof(AttackTask).Name))
            {
                foreach (var commander in MicroTaskData[typeof(AttackTask).Name].UnitCommanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)))
                {              
                    var unitType = commander.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Claimed = true;
                            commander.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander);
                        }
                    }
                }

                foreach (var commander in UnitCommanders)
                {
                    MicroTaskData[typeof(AttackTask).Name].StealUnit(commander);
                }
            }

            if (MicroTaskData.ContainsKey(typeof(DefenseSquadTask).Name))
            {
                foreach (var commander in MicroTaskData[typeof(DefenseSquadTask).Name].UnitCommanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)))
                {
                    var unitType = commander.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Claimed = true;
                            commander.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander);
                        }
                    }
                }

                foreach (var commander in UnitCommanders)
                {
                    MicroTaskData[typeof(DefenseSquadTask).Name].StealUnit(commander);
                }
            }
        }

        void StealFromMiningTask()
        {
            if (NextBaseLocation == null) { return; }
            var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);
            if (MicroTaskData.ContainsKey(typeof(MiningTask).Name))
            {
                foreach (var commander in MicroTaskData[typeof(MiningTask).Name].UnitCommanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)))
                {
                    commander.Claimed = true;
                    commander.UnitRole = UnitRole.Defend;
                    UnitCommanders.Add(commander);
                    if (UnitCommanders.Count(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker)) > 1)
                    {
                        break;
                    }
                }

                foreach (var commander in UnitCommanders)
                {
                    MicroTaskData[typeof(MiningTask).Name].StealUnit(commander);
                }
            }
        }

        private bool UpdateBaseLocation()
        {
            if (BaseData.SelfBases == null) { return false; }
            var baseCount = BaseData.SelfBases.Count();
            if (NextBaseLocation == null || BaseCountDuringLocation != baseCount)
            {
                var nextBase = BuildingService.GetNextBaseLocation();
                if (nextBase != null)
                {
                    NextBaseLocation = nextBase.Location;
                    BaseCountDuringLocation = baseCount;
                    BlockingMinerals = BuildingService.GetMineralsBlockingNextBase().ToList();
                    if (BlockingMinerals.Any())
                    {
                        StealFromMiningTask();
                    }
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                Deaths += UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
