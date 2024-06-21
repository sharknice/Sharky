namespace Sharky.MicroTasks.Zerg
{
    public class MutaliskSubAttackTask : AttackSubTask
    {
        TargetingService TargetingService;
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        AttackData AttackData;
        SharkyUnitData SharkyUnitData;
        MapDataService MapDataService;
        FrameToTimeConverter FrameToTimeConverter;
        MacroData MacroData;
        UnitCountService UnitCountService;

        List<MicroGroup> Groups;

        MutaliskGroupMicroController MutaliskMicroController;

        private int groupSize;

        public bool AllSupport { get; set; }
        public bool AlwaysAttack { get; set; }
        public int GroupSize
        {
            get { return groupSize; }
            set
            {
                groupSize = value;
                ReformGroups();
            }
        }

        /// <summary>
        /// Units estimated to be killed by mutalisk group, enemy units that die within 7 range of any mutalisks in task
        /// </summary>
        public Dictionary<ulong, UnitCalculation> Kills { get; private set; }

        public int MinimumAttackSize { get; set; }

        public bool SwitchRolesAfterWorkersMassacred { get; set; }
        public bool SwitchRolesAfterEnemyStaticDefenseEstablished { get; set; }

        // TODO: distract role that attacks enemy furthest away from it's army

        public MutaliskSubAttackTask(DefaultSharkyBot defaultSharkyBot, IAttackTask parentTask, MutaliskGroupMicroController mutaliskMicroController, int minimumAttackSize,
            float priority, bool enabled = false, bool alwaysAttack = true)
        {
            ParentTask = parentTask;

            MicroTaskData = defaultSharkyBot.MicroTaskData;
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            AttackData = defaultSharkyBot.AttackData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MapDataService = defaultSharkyBot.MapDataService;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            MacroData = defaultSharkyBot.MacroData;
            UnitCountService = defaultSharkyBot.UnitCountService;

            MutaliskMicroController = mutaliskMicroController;

            TargetingService = defaultSharkyBot.TargetingService;

            Priority = priority;
            Enabled = enabled;

            UnitCommanders = new List<UnitCommander>();
            Groups = new List<MicroGroup>();
            GroupSize = 20;
            MinimumAttackSize = minimumAttackSize;

            ArmySplitter = new ArmySplitter(defaultSharkyBot);
            AlwaysAttack = alwaysAttack;
            AllSupport = false;
            Kills = new Dictionary<ulong, UnitCalculation>();
            SwitchRolesAfterWorkersMassacred = true;
            SwitchRolesAfterEnemyStaticDefenseEstablished = true;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            ClaimCommanders(commanders.Values);
        }

        public override void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders)
        {
            ClaimCommanders(commanders);
        }

        void ClaimCommanders(IEnumerable<UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Claimed)
                {
                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_MUTALISK)
                    {
                        commander.Claimed = true;
                        commander.UnitRole = UnitRole.Attack;

                        UnitCommanders.Add(commander);

                        var group = GetGroupLookingForMore();
                        group.Commanders.Add(commander);
                    }
                }
            }
        }

        MicroGroup GetGroupLookingForMore()
        {
            var group = Groups.FirstOrDefault(g => g.Commanders.Count() < GroupSize);
            if (group == null)
            {
                group = new MicroGroup { Commanders = new List<UnitCommander>(), GroupRole = GroupRole.None };
                Groups.Add(group);
            }
            return group;
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            var deaths = 0;
            var kills = 0;
            foreach (var tag in deadUnits)
            {
                if (UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag) > 0)
                {
                    deaths++;
                    foreach (var group in Groups)
                    {
                        group.Commanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                    }
                }
                else
                {
                    var kill = UnitCommanders.Where(c => c.UnitCalculation.PreviousUnitCalculation?.NearbyEnemies != null).SelectMany(c => c.UnitCalculation.PreviousUnitCalculation.NearbyEnemies.Where(e => Vector2.DistanceSquared(c.UnitCalculation.Position, e.Position) < 50)).FirstOrDefault(e => e.Unit.Tag == tag);
                    if (kill != null && !SharkyUnitData.UndeadTypes.Contains((UnitTypes)kill.Unit.UnitType))
                    {
                        kills++;
                        Kills[tag] = kill;
                    }
                }
            }

            if (deaths > 0)
            {
                Deaths += deaths;
                Groups.RemoveAll(g => g.Commanders.Count() == 0);
                ReformGroups();
            }
            if (kills > 0 || deaths > 0)
            {
                ReportResults();

                if (SwitchRolesAfterWorkersMassacred && Groups.Any(g => g.GroupRole == GroupRole.HarassMineralLines) && Kills.Values.Count(k => k.UnitClassifications.HasFlag(UnitClassification.Worker)) > 20 && ActiveUnitData.EnemyUnits.Values.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) < 3)
                {
                    foreach (var group in Groups)
                    {
                        if (group.GroupRole == GroupRole.HarassMineralLines)
                        {
                            group.GroupRole = GroupRole.Attack;
                        }
                    }
                }
            }
        }

        public override void ResetClaimedUnits()
        {
            Groups.Clear();
            base.ResetClaimedUnits();
        }

        private void ReformGroups()
        {
            // TODO: combine or divide groups if needed
        }

        private Point2D GetGroupAttackPoint(MicroGroup group, int frame)
        {
            if ((AllSupport || group.GroupRole == GroupRole.Support || group.Commanders.Any(c => c.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen > frame - 5 && (e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE || e.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER)))) && group.Commanders.Any())
            {
                var vector = group.Commanders.FirstOrDefault().UnitCalculation.Position;
                var nearbyUnit = ActiveUnitData.Commanders.Values.Where(u => !u.UnitCalculation.Unit.IsHallucination && u.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_OBSERVER && u.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_ORACLE && u.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_DARKTEMPLAR && u.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType != (uint)UnitTypes.PROTOSS_PHOENIX) && !UnitCommanders.Contains(u)).OrderBy(u => Vector2.DistanceSquared(vector, u.UnitCalculation.Position)).FirstOrDefault();
                if (nearbyUnit != null)
                {
                    var position = nearbyUnit.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.UnitType != (uint)UnitTypes.PROTOSS_PHOENIX).Position;
                    return new Point2D { X = position.X, Y = position.Y };
                }
                if (AttackData.ArmyPoint != null)
                {
                    return AttackData.ArmyPoint;
                }
                return TargetingData.MainDefensePoint;
            }

            if (group.GroupRole == GroupRole.HarassMineralLines)
            {
                var mineralLine = BaseData.EnemyBases.Select(b => b.MineralLineLocation).OrderBy(p => MapDataService.LastFrameVisibility(p)).FirstOrDefault();
                if (mineralLine != null)
                {
                    return mineralLine;
                }
                if (MapDataService.LastFrameVisibility(BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation) < 100)
                {
                    return BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation;
                }            
            }

            if (group.GroupRole == GroupRole.AttackMain)
            {
                if (MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) == 0)
                {
                    return TargetingData.EnemyMainBasePoint;
                }
                else if (BaseData.EnemyBases.Any())
                {
                    return BaseData.EnemyBases.FirstOrDefault().Location;
                }
            }

            return TargetingData.AttackPoint;
        }

        GroupRole GetNextRole()
        {
            if (AllSupport)
            {
                return GroupRole.Support;
            }
            if (!Groups.Any(g => g.GroupRole == GroupRole.HarassMineralLines))
            {
                return GroupRole.HarassMineralLines;
            }
            if (!Groups.Any(g => g.GroupRole == GroupRole.AttackMain))
            {
                return GroupRole.AttackMain;
            }
            if (!Groups.Any(g => g.GroupRole == GroupRole.Support))
            {
                return GroupRole.Support;
            }
            if (!Groups.Any(g => g.GroupRole == GroupRole.Attack))
            {
                return GroupRole.Attack;
            }

            return GroupRole.None;
        }

        void UpdateStates()
        {
            if (SwitchRolesAfterEnemyStaticDefenseEstablished && Groups.Any(g => g.GroupRole != GroupRole.Support) && UnitCountService.EnemyCompleted(UnitTypes.TERRAN_MISSILETURRET) + UnitCountService.EnemyCompleted(UnitTypes.ZERG_SPORECRAWLER) + UnitCountService.EnemyCompleted(UnitTypes.PROTOSS_PHOTONCANNON) > 1)
            {
                foreach (var group in Groups)
                {
                    group.GroupRole = GroupRole.Support;
                }
            }

            foreach (var group in Groups)
            {
                if (group.Commanders.Count() < MinimumAttackSize)
                {
                    foreach (var commander in group.Commanders)
                    {
                        commander.UnitRole = UnitRole.Hide;
                    }
                }
                else if (group.Commanders.All(c => c.UnitRole == UnitRole.Regenerate))
                {
                    if (group.Commanders.All(c => c.UnitCalculation.Unit.Health > c.UnitCalculation.Unit.HealthMax * .9f))
                    {
                        foreach (var commander in group.Commanders)
                        {
                            commander.UnitRole = UnitRole.Attack;
                        }
                    }
                }
                else if (group.Commanders.Sum(c => c.UnitCalculation.Unit.Health) < group.Commanders.Sum(c => c.UnitCalculation.Unit.HealthMax) * .75f)
                {
                    ReportResults();
                    foreach (var commander in group.Commanders)
                    {
                        commander.UnitRole = UnitRole.Regenerate;
                    }
                }
                else
                {
                    var groupCenter = TargetingService.GetArmyPoint(group.Commanders);
                    var groupVector = new Vector2(groupCenter.X, groupCenter.Y);
                    var leader = group.Commanders.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
                    if (leader == null)
                    {
                        leader = group.Commanders.Where(c => c.UnitCalculation.Unit.Health > c.UnitCalculation.Unit.HealthMax * .75f).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, groupVector)).FirstOrDefault();
                        if (leader != null)
                        {
                            leader.UnitRole = UnitRole.Leader;
                        }
                    }
                    foreach (var commander in group.Commanders)
                    {
                        if (commander.UnitCalculation.Unit.Health <= commander.UnitCalculation.Unit.HealthMax * .75f)
                        {
                            commander.UnitRole = UnitRole.Regenerate;
                            commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.FullRetreat;
                        }
                        else if ((commander.UnitRole == UnitRole.Regenerate || commander.UnitRole == UnitRole.Hide) && commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax * .9f)
                        {
                            commander.UnitRole = UnitRole.Regroup;
                        }
                        else if (commander.UnitRole == UnitRole.Regroup && Vector2.DistanceSquared(commander.UnitCalculation.Position, groupVector) < 100)
                        {
                            commander.UnitRole = UnitRole.Attack;
                        }
                    }
                }
            }
        }

        private void ReportResults()
        {
            Console.WriteLine($"{FrameToTimeConverter.GetTime(MacroData.Frame)} Mutalisk Report: Deaths:{Deaths}, Kills:{Kills.Count()}");
            foreach (var killGroup in Kills.Values.GroupBy(k => k.Unit.UnitType))
            {
                Console.WriteLine($"{(UnitTypes)killGroup.Key}: {killGroup.Count()}");
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> Attack(Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {   
                if (group?.Commanders.FirstOrDefault() == null)
                {
                    continue;
                }

                if (group.GroupRole == GroupRole.None)
                {
                    group.GroupRole = GetNextRole();
                }
                var groupAttackPoint = GetGroupAttackPoint(group, frame);
                var retreatSpot = GetRegenerationSpot(group, defensePoint, armyPoint, frame);
                var groupCenter = TargetingService.GetArmyPoint(group.Commanders.Where(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader));


                if (group.Commanders.FirstOrDefault().UnitCalculation.TargetPriorityCalculation.AirWinnability > .5f)
                {
                    if (group.Commanders.Count(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader) >= MinimumAttackSize)
                    {
                        if (!AllSupport && (group.GroupRole == GroupRole.HarassMineralLines || group.GroupRole == GroupRole.Harass))
                        {
                            actions.AddRange(MutaliskMicroController.Attack(group.Commanders, groupAttackPoint, retreatSpot, groupCenter, frame, true));
                        }
                        else
                        {
                            actions.AddRange(MutaliskMicroController.Attack(group.Commanders, groupAttackPoint, retreatSpot, groupCenter, frame, false));
                        }
                    }
                    else
                    {
                        actions.AddRange(MutaliskMicroController.Retreat(group.Commanders, retreatSpot, groupCenter, frame));
                    }
                }
                else
                {
                    actions.AddRange(MutaliskMicroController.Retreat(group.Commanders, retreatSpot, groupCenter, frame));
                }            
            }

            return actions;
        }

        Point2D GetRegenerationSpot(MicroGroup group, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            var retreatSpot = defensePoint;
            var firstUnit = group.Commanders.FirstOrDefault();
            if (group.Commanders.FirstOrDefault().UnitRole == UnitRole.Hide)
            {
                // TODO: find best hiding spot near own base
                return BaseData.MainBase.BehindMineralLineLocation;
            }
            else
            {
                var closestSafeBase = BaseData?.EnemyBaseLocations?.OrderBy(b => Vector2.DistanceSquared(armyPoint.ToVector2(), b.Location.ToVector2())).FirstOrDefault(b => b.ResourceCenter == null && !MapDataService.InEnemyVision(b.Location));
                if (closestSafeBase != null)
                {
                    return closestSafeBase.Location;
                }
            }

            return retreatSpot;
        }

        public override IEnumerable<SC2APIProtocol.Action> Retreat(Point2D defensePoint, Point2D armyPoint, int frame)
        {
            if (AlwaysAttack) { return Attack(TargetingData.AttackPoint, defensePoint, armyPoint, frame); }

            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var retreatPoint = defensePoint;
                if (group.Commanders.FirstOrDefault().UnitRole == UnitRole.Hide)
                {
                    retreatPoint = BaseData.MainBase.BehindMineralLineLocation;
                }

                var groupCenter = TargetingService.GetArmyPoint(group.Commanders);
                actions.AddRange(MutaliskMicroController.Retreat(group.Commanders, retreatPoint, groupCenter, frame));
            }

            return actions;
        }

        public override IEnumerable<SC2APIProtocol.Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var groupCenter = TargetingService.GetArmyPoint(group.Commanders);
                actions.AddRange(MutaliskMicroController.Support(group.Commanders, mainUnits, attackPoint, defensivePoint, groupCenter, frame));
            }

            return actions;
        }

        public override IEnumerable<SC2APIProtocol.Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            if (AlwaysAttack) { return Support(mainUnits, attackPoint, defensivePoint, armyPoint, frame); }

            UpdateStates();

            var actions = new List<SC2APIProtocol.Action>();

            foreach (var group in Groups)
            {
                var groupCenter = TargetingService.GetArmyPoint(group.Commanders);
                actions.AddRange(MutaliskMicroController.Support(group.Commanders, mainUnits, defensivePoint, defensivePoint, groupCenter, frame));
            }

            return actions;
        }

        public override IEnumerable<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint)
        {
            return Attack(closerEnemies.FirstOrDefault().Position.ToPoint2D(), defensePoint, armyPoint, frame);
        }
    }
}
