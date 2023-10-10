namespace Sharky.MicroTasks
{
    public class DefenseSquadTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;
        EnemyData EnemyData;
        UnitDataService UnitDataService;

        DefenseService DefenseService;

        IMicroController MicroController;

        ArmySplitter ArmySplitter;

        float lastFrameTime;

        public bool OnlyDefendMain { get; set; }
        public bool GroupAtMain { get; set; }
        public bool AlwaysFillBunkers { get; set; }

        List<UnitCommander> WorkerDefenders { get; set; }

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        public DefenseSquadTask(DefaultSharkyBot defaultSharkyBot,
            ArmySplitter armySplitter,
            List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            EnemyData = defaultSharkyBot.EnemyData;
            UnitDataService = defaultSharkyBot.UnitDataService;

            DefenseService = defaultSharkyBot.DefenseService;

            MicroController = defaultSharkyBot.MicroController;

            ArmySplitter = armySplitter;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            WorkerDefenders = new List<UnitCommander>();

            OnlyDefendMain = false;
            GroupAtMain = false;
            AlwaysFillBunkers = true;

            Enabled = true;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && NeedDesiredClaim(desiredUnitClaim))
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        bool NeedDesiredClaim(DesiredUnitsClaim desiredUnitClaim)
        {
            var count = UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType);
            if (desiredUnitClaim.UnitType == UnitTypes.TERRAN_SIEGETANK)
            {
                count+= UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
            }
            return count < desiredUnitClaim.Count;
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var actions = new List<SC2Action>();

            if (lastFrameTime > 5)
            {
                lastFrameTime = 0;
                return actions;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            FillBunkers(frame, actions);

            var structures = ActiveUnitData.SelfUnits.Where(u => u.Value.Attributes.Contains(SC2Attribute.Structure));
            if (OnlyDefendMain)
            {
                var vector = new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y);
                structures = structures.Where(u => Vector2.DistanceSquared(u.Value.Position, vector) < 400);
            }
            var attackingEnemies = structures.SelectMany(u => u.Value.NearbyEnemies).Distinct().Where(e => ActiveUnitData.EnemyUnits.ContainsKey(e.Unit.Tag));
            if (attackingEnemies.Any())
            {
                if (UnitCommanders.Count() == 0)
                {
                    actions.AddRange(DefendWithWorkers(attackingEnemies, frame));
                }
                else
                {
                    StopDefendingWithWorkers();
                }

                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat)
                    {
                        commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.Attack;
                    }
                }
                actions.AddRange(ArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.MainDefensePoint, UnitCommanders, true, true));
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }
            else
            {
                var defensePoint = TargetingData.ForwardDefensePoint;
                if (OnlyDefendMain || GroupAtMain)
                {
                    defensePoint = TargetingData.SelfMainBasePoint;
                }
                actions = MicroController.Retreat(UnitCommanders, defensePoint, null, frame);
            }
            StopDefendingWithWorkers();
            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void FillBunkers(int frame, List<SC2APIProtocol.Action> actions)
        {
            if (AlwaysFillBunkers && EnemyData.SelfRace == Race.Terran)
            {
                foreach (var commander in UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_MARINE && !c.UnitCalculation.EnemiesThreateningDamage.Any()))
                {
                    var nearbyBunkers = ActiveUnitData.SelfUnits.Values.Where(c => c.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && c.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.Position, TargetingData.ForwardDefensePoint.ToVector2()));
                    foreach (var bunker in nearbyBunkers)
                    {
                        if (bunker.Unit.CargoSpaceMax - bunker.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                        {
                            var action = commander.Order(frame, Abilities.SMART, targetTag: bunker.Unit.Tag, allowSpam: true);
                            actions.AddRange(action);
                            break;
                        }
                    }
                }
            }
        }

        private void StopDefendingWithWorkers()
        {
            if (WorkerDefenders.Any())
            {
                foreach (var defender in WorkerDefenders)
                {
                    defender.UnitRole = UnitRole.None;
                }
                WorkerDefenders.Clear();
            }
        }

        private List<SC2Action> DefendWithWorkers(IEnumerable<UnitCalculation> attackingEnemies, int frame)
        {
            var bunkersInProgress = attackingEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && (e.Unit.BuildProgress < 1 || (e.Unit.HasHealth && e.Unit.Health < 100)) && Vector2.DistanceSquared(e.Position, new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y)) < 1600);
            if (bunkersInProgress.Any())
            {
                var bunker = bunkersInProgress.OrderByDescending(u => u.Unit.BuildProgress).FirstOrDefault();
                // attack with 8 workers
                if (WorkerDefenders.Count() == 0)
                {
                    var closestWokrers = ActiveUnitData.Commanders.Where(u => u.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && u.Value.UnitRole == UnitRole.Minerals).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, bunker.Position)).Take(7 + bunker.NearbyAllies.Count());
                    WorkerDefenders.AddRange(closestWokrers.Select(c => c.Value));
                    foreach (var worker in WorkerDefenders)
                    {
                        worker.UnitRole = UnitRole.Attack;
                    }
                }
                foreach (var worker in WorkerDefenders.Where(w => w.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || w.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
                {
                    worker.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillBunker;
                }

                return MicroController.Attack(WorkerDefenders, new Point2D { X = bunker.Position.X, Y = bunker.Position.Y }, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, frame);
            }

            return new List<SC2Action>();
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                WorkerDefenders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
