namespace Sharky.MicroTasks.Attack
{
    public class AdvancedAttackTask : MicroTask, IAttackTask
    {
        AttackData AttackData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MicroTaskData MicroTaskData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;

        IMicroController MicroController;

        TargetingService TargetingService;
        EnemyCleanupService EnemyCleanupService;
        UnitCountService UnitCountService;

        ArmySplitter DefenseArmySplitter;
        ArmySplitter AttackArmySplitter;
        CameraManager CameraManager;

        public List<UnitTypes> MainAttackers { get; set; }
        public List<UnitCommander> MainUnits { get; set; }
        public List<UnitCommander> SupportUnits { get; set; }
        UnitCommander NextLeader { get; set; }

        public Dictionary<string, IAttackSubTask> SubTasks { get; set; }

        List<UnitCalculation> EnemyAttackers { get; set; }

        public bool OnlyDefendBuildings { get; set; }
        public bool AllowSplitWhileKill { get; set; }
        public bool DeathBallMode { get; set; }

        bool BaseUnderAttack { get; set; }


        public AdvancedAttackTask(DefaultSharkyBot defaultSharkyBot, EnemyCleanupService enemyCleanupService, List<UnitTypes> mainAttackerTypes, float priority, bool enabled = true)
        {
            AttackData = defaultSharkyBot.AttackData;
            TargetingData = defaultSharkyBot.TargetingData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            MacroData = defaultSharkyBot.MacroData;
            MicroController = defaultSharkyBot.MicroController;
            TargetingService = defaultSharkyBot.TargetingService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            CameraManager = defaultSharkyBot.CameraManager;

            EnemyCleanupService = enemyCleanupService;

            DefenseArmySplitter = new ArmySplitter(defaultSharkyBot);
            AttackArmySplitter = new ArmySplitter(defaultSharkyBot);

            MainAttackers = mainAttackerTypes;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            MainUnits = new List<UnitCommander>();
            SupportUnits = new List<UnitCommander>();

            EnemyAttackers = new List<UnitCalculation>();

            SubTasks = new Dictionary<string, IAttackSubTask>();

            AllowSplitWhileKill = true;
        }

        public override void ResetClaimedUnits()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();
            MainUnits = new List<UnitCommander>();
            SupportUnits = new List<UnitCommander>();
            foreach (var subtask in SubTasks.OrderBy(t => t.Value.Priority))
            {
                subtask.Value.ResetClaimedUnits();
            }
        }

        public override List<UnitCommander> ResetNonEssentialClaims()
        {
            var removals = UnitCommanders.Where(c => c.UnitRole != UnitRole.Leader && !SubTasks.Any(s => s.Value.UnitCommanders.Contains(c))).ToList();

            foreach (var removal in removals)
            {
                UnitCommanders.Remove(removal);
                MainUnits.Remove(removal);
                SupportUnits.Remove(removal);
                removal.UnitRole = UnitRole.None;
                removal.Claimed = false;
            }

            return removals;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (MainAttackers.Any() && !MainUnits.Any())
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed)
                    {
                        if (MainAttackers.Contains((UnitTypes)commander.Value.UnitCalculation.Unit.UnitType) && !commander.Value.UnitCalculation.Unit.IsHallucination)
                        {
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            commander.Value.UnitRole = UnitRole.Leader;
                            MainUnits.Add(commander.Value);
                            break;
                        }
                    }
                }
            }

            foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
            {
                subTask.Value.ClaimUnits(commanders);
            }

            var desiredScvs = CalculateDesiredScvs();
            var claimedScvs = UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    if (commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit) || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED)
                    {
                        CameraManager.SetCamera(commander.Value.UnitCalculation.Position);

                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);

                        var idealLeader = MainAttackers.FirstOrDefault();
                        if (commander.Value.UnitCalculation.Unit.UnitType == (uint)idealLeader && !MainUnits.Any(c => c.UnitCalculation.Unit.UnitType == (uint)idealLeader))
                        {
                            commander.Value.UnitRole = UnitRole.NextLeader;
                            SupportUnits.Add(commander.Value);
                            NextLeader = commander.Value;
                        }
                        else if (!MainUnits.Any() && MainAttackers.Contains((UnitTypes)commander.Value.UnitCalculation.Unit.UnitType) && !commander.Value.UnitCalculation.Unit.IsHallucination)
                        {
                            commander.Value.UnitRole = UnitRole.Leader;
                            MainUnits.Add(commander.Value);
                        }
                        else
                        {
                            commander.Value.UnitRole = UnitRole.None;
                            SupportUnits.Add(commander.Value);
                        }
                    }
                    else if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && claimedScvs < desiredScvs)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Support;
                        UnitCommanders.Add(commander.Value);
                        SupportUnits.Add(commander.Value);
                        claimedScvs++;
                    }
                }
            }

            ClaimBusyScvs(commanders, desiredScvs, claimedScvs);
            UnclaimScvs(desiredScvs, claimedScvs);

            foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
            {
                subTask.Value.ClaimUnitsFromParent(GetAvailableCommanders());
            }

            var stuck = MainUnits.FirstOrDefault(c => c.CommanderState == CommanderState.Stuck);
            if (stuck != null)
            {
                System.Console.WriteLine($"AdvancedAttackTask: removing stuck {(UnitTypes)stuck.UnitCalculation.Unit.UnitType} #{stuck.UnitCalculation.Unit.Tag} from MainUnits");
                MainUnits.Remove(stuck);
                SupportUnits.Add(stuck);
            }
        }

        void ClaimBusyScvs(Dictionary<ulong, UnitCommander> commanders, int desiredScvs, int claimedScvs)
        {
            if (claimedScvs < desiredScvs)
            {
                var targetVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
                foreach (var commander in commanders.Where(c => c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && (c.Value.UnitRole == UnitRole.Minerals || c.Value.UnitRole == UnitRole.None) && !c.Value.UnitCalculation.Unit.BuffIds.Any()).OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, targetVector)))
                {
                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Support;
                    UnitCommanders.Add(commander.Value);
                    SupportUnits.Add(commander.Value);
                    claimedScvs++;
                    if (claimedScvs >= desiredScvs)
                    {
                        return;
                    }
                }
            }
        }

        void UnclaimScvs(int desiredScvs, int claimedScvs)
        {
            if (claimedScvs > desiredScvs || !AttackData.Attacking && !UnitCommanders.Any(c => c.UnitCalculation.Unit.Health < c.UnitCalculation.Unit.HealthMax && c.UnitCalculation.Attributes.Contains(SC2Attribute.Mechanical)) || MacroData.Minerals == 0)
            {
                var scv = UnitCommanders.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
                if (scv != null)
                {
                    MainUnits.Remove(scv);
                    SupportUnits.Remove(scv);
                    UnitCommanders.Remove(scv);
                    scv.Claimed = false;
                    scv.UnitRole = UnitRole.None;
                }
            }
        }

        int CalculateDesiredScvs()
        {
            var totalScvs = UnitCountService.Count(UnitTypes.TERRAN_SCV);
            if (MacroData.Minerals > 50)
            {
                if (AttackData.Attacking)
                {
                    var desiredTotal = (UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_THOR || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_THORAP || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BATTLECRUISER) * 3) +
                        UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
                    if (totalScvs - desiredTotal < 22)
                    {
                        desiredTotal = totalScvs - 22;
                    }
                    if (desiredTotal > 15 && MacroData.FoodUsed < 185)
                    {
                        desiredTotal = 15;
                    }
                    return desiredTotal;
                }
                else
                {
                    var missingHealth = UnitCommanders.Where(c => c.UnitCalculation.Attributes.Contains(SC2Attribute.Mechanical)).Sum(c => c.UnitCalculation.Unit.HealthMax - c.UnitCalculation.Unit.Health);
                    if (missingHealth > 0)
                    {
                        var desiredTotal = (missingHealth / 50f) + 1;
                        if (totalScvs - desiredTotal < 22)
                        {
                            desiredTotal = totalScvs - 22;
                        }
                        if (desiredTotal > 15)
                        {
                            desiredTotal = 15;
                        }
                        return (int)desiredTotal;
                    }
                }
            }

            return 0;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (!UnitCommanders.Any() && !SubTasks.Any()) { return actions; }

            UpdateLeader();

            if (MainUnits.Any())
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(MainUnits);
            }
            else
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(SupportUnits);
            }
            TargetingData.AttackPoint = TargetingService.UpdateAttackPoint(AttackData.ArmyPoint, TargetingData.AttackPoint);

            var attackingEnemies = ActiveUnitData.EnemyUnits.Values.Where(e => e.FrameLastSeen > frame - 100 &&
                    (e.NearbyEnemies.Any(u => u.Attributes.Contains(SC2Attribute.Structure) && u.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && u.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMOR && u.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE && u.Unit.UnitType != (uint)UnitTypes.PROTOSS_ORACLESTASISTRAP && u.UnitTypeData.Attributes.Contains(SC2Attribute.Structure)) ||
                    (e.TargetPriorityCalculation.OverallWinnability < .5f && EnemyAttackers.Any(ea => ea.Unit.Tag == e.Unit.Tag))
                ) && (e.NearbyEnemies.Count(b => b.Attributes.Contains(SC2Attribute.Structure)) >= e.NearbyAllies.Count(b => b.Attributes.Contains(SC2Attribute.Structure)))
            );

            if (OnlyDefendBuildings)
            {
                attackingEnemies = ActiveUnitData.EnemyUnits.Values.Where(e => e.FrameLastSeen > frame - 100 && e.NearbyEnemies.Any(u => (u.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.UnitClassifications.Contains(UnitClassification.ProductionStructure) || u.UnitClassifications.Contains(UnitClassification.DefensiveStructure)) && u.EnemiesThreateningDamage.Any()));
            }

            if (DeathBallMode)
            {
                attackingEnemies = attackingEnemies.Where(e => e.Damage > 0 && e.EnemiesInRange.Any() && !e.Unit.IsHallucination && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZEALOT && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGMARINE && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGMARINESHIELD && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZERGLING && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZERGLINGWINGS);
            }

            var attackPoint = TargetingData.AttackPoint;
            BaseUnderAttack = false;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (attackingEnemies.Any())
            {
                var armyVector = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                var distanceToAttackPoint = Vector2.DistanceSquared(armyVector, attackPoint.ToVector2());
                var closerEnemies = attackingEnemies;
                if (AttackData.Attacking)
                {
                    closerEnemies = attackingEnemies.Where(e => Vector2.DistanceSquared(e.Position, armyVector) < distanceToAttackPoint);
                }
                var defendToDeath = MacroData.FoodUsed > 175;
                if (closerEnemies.Any())
                {
                    if (DeathBallMode)
                    {
                        var baseUnderAttack = ActiveUnitData.SelfUnits.Values.Where(a => (a.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && a.EnemiesInRangeOf.Any()) || (a.UnitClassifications.Contains(UnitClassification.ResourceCenter) && a.NearbyEnemies.Any(e => e.Damage > 0)));
                        if (baseUnderAttack.Any())
                        {
                            attackPoint = baseUnderAttack.OrderBy(e => Vector2.DistanceSquared(e.Position, armyVector)).FirstOrDefault().Position.ToPoint2D();
                            BaseUnderAttack = true;
                        }
                        else
                        {
                            attackPoint = closerEnemies.OrderBy(e => Vector2.DistanceSquared(e.Position, armyVector)).FirstOrDefault().Position.ToPoint2D();
                        }
                        if (stopwatch.ElapsedMilliseconds > 100)
                        {
                            System.Console.WriteLine($"AdvancedAttackTask DeathBallMode attackPoint {stopwatch.ElapsedMilliseconds}");
                        }
                    }
                    else
                    {
                        actions = DefenseArmySplitter.SplitArmy(frame, closerEnemies, attackPoint, MainUnits.Concat(SupportUnits), defendToDeath);
                        if (stopwatch.ElapsedMilliseconds > 100)
                        {
                            System.Console.WriteLine($"AdvancedAttackTask closerenemies splitarmy {stopwatch.ElapsedMilliseconds}");
                        }
                        foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                        {
                            var subActions = subTask.Value.SplitArmy(frame, closerEnemies, attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint);
                            actions.AddRange(subActions);
                        }

                        stopwatch.Stop();
                        if (stopwatch.ElapsedMilliseconds > 100)
                        {
                            System.Console.WriteLine($"AdvancedAttackTask closerenemies {stopwatch.ElapsedMilliseconds}");
                        }
                        stopwatch.Restart();

                        RemoveTemporaryUnits();
                        UpdateEnemyAttackers(frame, attackingEnemies);
                        return actions;
                    }
                }
                else if (!DeathBallMode)
                {
                    var attackVector = attackPoint.ToVector2();
                    var closerSelfUnits = SupportUnits.Where(u => attackingEnemies.Any(e => Vector2.DistanceSquared(u.UnitCalculation.Position, attackVector) > Vector2.DistanceSquared(u.UnitCalculation.Position, e.Position)));
                    if (closerSelfUnits.Any())
                    {
                        actions.AddRange(DefenseArmySplitter.SplitArmy(frame, attackingEnemies, attackPoint, closerSelfUnits, defendToDeath));
                    }
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        System.Console.WriteLine($"AdvancedAttackTask attackingenemies splitarmy {stopwatch.ElapsedMilliseconds}");
                    }
                }

                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    System.Console.WriteLine($"AdvancedAttackTask attackingenemies end {stopwatch.ElapsedMilliseconds}");
                }
            }
            stopwatch.Restart();

            UpdateEnemyAttackers(frame, attackingEnemies);

            HandleHiddenBuildings();

            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask updatestuff {stopwatch.ElapsedMilliseconds}");
            }
            stopwatch.Restart();


            if (MainUnits.Any(m => !m.UnitCalculation.Loaded))
            {
                OrderMainUnitsWithSupportUnits(frame, actions, MainUnits, SupportUnits, attackPoint);
            }
            else
            {
                OrderSupportUnitsWithoutMainUnits(frame, actions, SupportUnits, attackPoint);
            }

            RemoveTemporaryUnits();

            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask everything end {stopwatch.ElapsedMilliseconds}");
            }

            return actions;
        }

        private void UpdateLeader()
        {
            if (NextLeader != null)
            {
                var currentLeader = MainUnits.FirstOrDefault();
                if (currentLeader == null)
                {
                    SwapLeader(currentLeader);
                }
                else
                {
                    if (Vector2.DistanceSquared(NextLeader.UnitCalculation.Position, currentLeader.UnitCalculation.Position) < 36)
                    {
                        SwapLeader(currentLeader);
                    }
                }
            }
        }

        private void SwapLeader(UnitCommander currentLeader)
        {
            SupportUnits.Remove(NextLeader);
            NextLeader.UnitRole = UnitRole.Leader;
            MainUnits.Add(NextLeader);
            NextLeader = null;

            if (currentLeader != null) 
            {
                MainUnits.Remove(currentLeader);
                currentLeader.UnitRole = UnitRole.None;
                SupportUnits.Add(currentLeader);
            }
        }

        private void UpdateEnemyAttackers(int frame, IEnumerable<UnitCalculation> attackingEnemies)
        {
            if (DeathBallMode)
            {
                EnemyAttackers = new List<UnitCalculation>();
            }
            else
            {
                EnemyAttackers = attackingEnemies.Where(e => e.FrameLastSeen > frame - 100 && (e.EnemiesInRangeOf.Any() || e.EnemiesInRange.Any())).ToList();
            }
        }

        private void RemoveTemporaryUnits()
        {
            var undeadTypes = SharkyUnitData.UndeadTypes.Where(t => t != UnitTypes.PROTOSS_ADEPTPHASESHIFT && t != UnitTypes.PROTOSS_DISRUPTORPHASED);
            UnitCommanders.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));
            SupportUnits.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));
            MainUnits.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));

            if (!MainUnits.Any())
            {
                foreach (var mainType in MainAttackers)
                {
                    var mainUnit = SupportUnits.FirstOrDefault(commander => (UnitTypes)commander.UnitCalculation.Unit.UnitType == mainType && !commander.UnitCalculation.Unit.IsHallucination);
                    if (mainUnit != null)
                    {
                        SupportUnits.Remove(mainUnit);
                        mainUnit.UnitRole = UnitRole.Leader;
                        MainUnits.Add(mainUnit);
                        break;
                    }
                }

            }
        }

        private void OrderSupportUnitsWithoutMainUnits(int frame, List<SC2Action> actions, IEnumerable<UnitCommander> supportUnits, Point2D attackPoint)
        {
            var stopwatch = Stopwatch.StartNew();

            if (DeathBallDefending(attackPoint))
            {
                actions.AddRange(MicroController.Attack(supportUnits, attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.Attack(attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
            }
            else if (AttackData.Attacking)
            {
                if (TargetingData.AttackState == AttackState.Kill || !AttackData.UseAttackDataManager || DeathBallMode)
                {
                    var attackingEnemies = supportUnits.SelectMany(c => c.UnitCalculation.NearbyEnemies).Distinct();
                    if (AllowSplitWhileKill && attackingEnemies.Any())
                    {
                        var splitActions = AttackArmySplitter.SplitArmy(frame, attackingEnemies, attackPoint, supportUnits, false);
                        actions.AddRange(splitActions);
                    }
                    else
                    {
                        actions.AddRange(MicroController.Attack(supportUnits, attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Attack(attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                }
                else if (TargetingData.AttackState == AttackState.Contain)
                {
                    actions.AddRange(MicroController.Retreat(supportUnits, attackPoint, AttackData.ArmyPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Retreat(attackPoint, AttackData.ArmyPoint, frame));
                    }
                }
            }
            else
            {
                if (DeathBallMode)
                {
                    actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
                else
                {
                    var cleanupActions = EnemyCleanupService.CleanupEnemies(supportUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                    if (cleanupActions != null)
                    {
                        actions.AddRange(cleanupActions);
                    }
                    else
                    {
                        actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                }
                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.Retreat(TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
            }

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask OrderSupportUnitsWithoutMainUnits {stopwatch.ElapsedMilliseconds}");
            }
        }

        private bool DeathBallDefending(Point2D attackPoint)
        {
            if (DeathBallMode)
            {
                if (BaseUnderAttack && !AttackData.Attacking) 
                { 
                    return true;
                }

                if (!(TargetingData.AttackPoint.X == attackPoint.X && TargetingData.AttackPoint.Y == attackPoint.Y))
                {
                    var leader = MainUnits.FirstOrDefault();
                    if (leader == null)
                    {
                        leader = UnitCommanders.FirstOrDefault();
                    }

                    if (leader != null)
                    {
                        return leader.UnitCalculation.TargetPriorityCalculation.Overwhelm;
                    }
                    return true;
                }
            }
            return false;
        }

        private void OrderMainUnitsWithSupportUnits(int frame, List<SC2Action> actions, IEnumerable<UnitCommander> mainUnits, IEnumerable<UnitCommander> supportUnits, Point2D attackPoint)
        {
            var stopwatch = Stopwatch.StartNew();

            var supportAttackPoint = attackPoint;
            var mainUnit = mainUnits.FirstOrDefault();
            if (mainUnit != null)
            {
                supportAttackPoint = mainUnit.UnitCalculation.Position.ToPoint2D();
            }

            if (DeathBallDefending(attackPoint))
            {
                actions.AddRange(MicroController.Retreat(mainUnits, attackPoint, AttackData.ArmyPoint, frame));
                actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.Support(mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
            }
            else if (AttackData.Attacking)
            {
                if (TargetingData.AttackState == AttackState.Kill || TargetingData.AttackState == AttackState.None || DeathBallMode)
                {
                    actions.AddRange(MicroController.Attack(mainUnits, attackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Support(mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, supportAttackPoint, supportAttackPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Support(mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                    }
                }
            }
            else
            {
                if (DeathBallMode)
                {
                    actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, null, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                }
                else
                {
                    var cleanupActions = EnemyCleanupService.CleanupEnemies(mainUnits.Concat(supportUnits), TargetingData.ForwardDefensePoint, supportAttackPoint, frame);
                    if (cleanupActions != null)
                    {
                        actions.AddRange(cleanupActions);
                    }
                    else
                    {
                        actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, null, frame));
                        actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                    }
                }

                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.SupportRetreat(mainUnits, supportAttackPoint, supportAttackPoint, null, frame));
                }
            }

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask OrderMainUnitsWithSupportUnits {stopwatch.ElapsedMilliseconds}");
            }
        }

        void HandleHiddenBuildings()
        {
            if (TargetingData.HiddenEnemyBase && !MicroTaskData[typeof(FindHiddenBaseTask).Name].Enabled)
            {
                ResetClaimedUnits();
                MicroTaskData[typeof(FindHiddenBaseTask).Name].Enable();
            }
            else if (!TargetingData.HiddenEnemyBase && MicroTaskData[typeof(FindHiddenBaseTask).Name].Enabled)
            {
                MicroTaskData[typeof(FindHiddenBaseTask).Name].Disable();
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                SupportUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                MainUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
            foreach (var subTask in SubTasks.Where(s => s.Value.Enabled))
            {
                subTask.Value.RemoveDeadUnits(deadUnits);
            }
        }

        public override void StealUnit(UnitCommander commander)
        {
            RemoveDeadUnits(new List<ulong> { commander.UnitCalculation.Unit.Tag });
        }

        public void GiveCommanderToChild(UnitCommander commander)
        {
            SupportUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag);
            MainUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag);
        }

        public IEnumerable<UnitCommander> GetAvailableCommanders()
        {
            return SupportUnits;
        }

        public override void PrintReport(int frame)
        {
            base.PrintReport(frame);

            Console.WriteLine("    Main Units:");
            foreach (var unit in MainUnits)
            {
                Console.WriteLine($"{unit.UnitCalculation.Unit.UnitType} {unit.UnitCalculation.Unit.Tag}");
            }

            if (DefenseArmySplitter.ArmySplits == null) { return; }

            System.Console.WriteLine($"    Defensive Splits:");
            foreach (var split in DefenseArmySplitter.ArmySplits)
            {
                System.Console.WriteLine($"    split:");
                System.Console.WriteLine($"     enemies:");
                var unitGroups = split.EnemyGroup.GroupBy(x => x.Unit.UnitType);
                foreach (var group in unitGroups.OrderBy(x => System.Enum.GetName(typeof(UnitTypes), x.Key)))
                {
                    System.Console.WriteLine($"     [{(UnitTypes)group.Key}]={group.Count()}");
                }
                System.Console.WriteLine($"     allies:");
                var commandGroups = split.SelfGroup.GroupBy(x => x.UnitCalculation.Unit.UnitType);
                foreach (var group in commandGroups.OrderBy(x => System.Enum.GetName(typeof(UnitTypes), x.Key)))
                {
                    System.Console.WriteLine($"     [{(UnitTypes)group.Key}]={group.Count()}");
                }
            }
        }

        public override void DebugUnits(DebugService debugService)
        {
            base.DebugUnits(debugService);
            foreach (var subTask in SubTasks)
            {
                subTask.Value.DebugUnits(debugService);
            }
        }
    }
}
