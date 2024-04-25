namespace Sharky.MicroTasks.Harass
{
    public class BansheeHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        ChatService ChatService;
        DebugService DebugService;
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;
        HarassPathingService HarassPathingService;
        FrameToTimeConverter FrameToTimeConverter;
        CameraManager CameraManager;

        IIndividualMicroController IndividualMicroController;

        public Dictionary<ulong, UnitCalculation> Kills { get; private set; }

        int DesiredCount { get; set; }
        bool started { get; set; }
        bool CheeseChatSent { get; set; }
        bool YoloChatSent { get; set; }

        Point2D Target { get; set; }

        int TargetIndex;

        PathData PathToEnemyMainData { get; set; }

        public BansheeHarassTask(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController microController, int desiredCount = 2, bool enabled = true, float priority = -1f)
        {
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            HarassPathingService = defaultSharkyBot.HarassPathingService;
            ChatService = defaultSharkyBot.ChatService;
            DebugService = defaultSharkyBot.DebugService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MacroData = defaultSharkyBot.MacroData;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            CameraManager = defaultSharkyBot.CameraManager;
            IndividualMicroController = microController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            TargetIndex = -1;

            Kills = new Dictionary<ulong, UnitCalculation>();
        }

        public override void Enable()
        {
            Enabled = true;
            started = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
                        if (TargetIndex == 0)
                        {
                            commander.Value.CurrentPath = PathToEnemyMainData;
                            commander.Value.CurrentPathIndex = 0;
                        }
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() == DesiredCount)
                        {
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (Target == null)
            {
                Target = BaseData.EnemyBaseLocations.FirstOrDefault().MiddleMineralLocation;
                GetTargetPath(Target, frame);
            }

            DebugService.DrawSphere(new Point { X = Target.X, Y = Target.Y, Z = 10 }, .5f);

            var defensivePoint = TargetingData.ForwardDefensePoint;

            foreach (var commander in UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE))
            {
                if (commander.UnitRole == UnitRole.Repair)
                {
                    if (commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.HealthMax)
                    {
                        commander.UnitRole = UnitRole.Harass;
                    }
                    var repairSpot = BaseData.SelfBases.FirstOrDefault();
                    if (repairSpot != null)
                    {
                        var action = IndividualMicroController.NavigateToPoint(commander, repairSpot.MineralLineLocation, TargetingData.MainDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
                {
                    commander.UnitRole = UnitRole.Repair;
                }

                if (commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.HealthMax || Undetected(commander) || !commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    if (commander.UnitCalculation.EnemiesInRange.Any(e => e.FrameLastSeen == frame && e.UnitClassifications.HasFlag(UnitClassification.Worker)) ||
                        (commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame && e.UnitClassifications.HasFlag(UnitClassification.Worker)) > 2 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir)))
                    {
                        // kill free workers
                        var action = IndividualMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                        if (action != null)
                        {
                            CameraManager.SetCamera(commander.UnitCalculation.Position);
                            commands.AddRange(action);
                        }
                        continue;
                    }
                    if (commander.UnitCalculation.Unit.WeaponCooldown == 0 && commander.UnitCalculation.EnemiesInRange.Any())
                    {
                        // do free damage kiting to target
                        var action = IndividualMicroController.Attack(commander, Target, defensivePoint, null, frame);
                        if (action != null)
                        {
                            CameraManager.SetCamera(commander.UnitCalculation.Position);
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                bool atTarget = Vector2.DistanceSquared(commander.UnitCalculation.Position, Target.ToVector2()) < 100;

                // avoid damage
                if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    if (frame % 50 == 0)
                    {
                        CameraManager.SetCamera(commander.UnitCalculation.Position);
                    }
                    if (!Undetected(commander) && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax)
                    {
                        var action = IndividualMicroController.NavigateToPoint(commander, Target, defensivePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (atTarget)
                        {
                            GetNextTarget(frame);
                        }
                        continue;
                    }
                }

                // follow path until get to target
                if (!atTarget)
                {
                    var point = HarassPathingService.GetNextPointToTarget(commander, Target);
                    DebugService.DrawLine(commander.UnitCalculation.Unit.Pos, new Point { X = point.X, Y = point.Y, Z = 16 }, new Color { R = 250, B = 250, G = 250 });
                    var action = commander.Order(frame, Abilities.MOVE, point);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                // at target

                // harass workers
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
                {
                    var action = IndividualMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                // move closer
                if (!MapDataService.SelfVisible(Target))
                {
                    var point = HarassPathingService.GetNextPointToTarget(commander, Target);
                    var action = commander.Order(frame, Abilities.MOVE, point);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                // if no nearby workers move on(if no activeunitdata any enemy workers on whole map, attack any enemies nearby), 
                if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
                {
                    // if nothing is here move on, or if workers elsewhere go kill them
                    if (!commander.UnitCalculation.NearbyEnemies.Any() || ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.HasFlag(UnitClassification.Worker)))
                    {
                        GetNextTarget(frame);
                        continue;
                    }
                }

                // just kill what's here
                var defaultAction = IndividualMicroController.Attack(commander, Target, defensivePoint, null, frame);
                if (defaultAction != null)
                {
                    commands.AddRange(defaultAction);
                }
            }

            return commands;
        }

        private void SendYoloChat()
        {
            if (!YoloChatSent)
            {
                ChatService.SendChatType("BansheeHarass-Dying");
                YoloChatSent = true;
            }
        }

        bool CanHarass(UnitCommander commander, Point2D target)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
            {
                return false;
            }
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }
            if (MapDataService.SelfVisible(new Point2D { X = target.X, Y = target.Y, }) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker) && (Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) <= 100)))
            {
                return false;
            }
            if (!Undetected(commander) && commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.DamageAir).Sum(e => e.Damage) * 3 > commander.UnitCalculation.Unit.Health)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// harass all the known enemy bases, or just go between the main and natural if there aren't any other enemy bases
        /// </summary>
        /// <param name="frame">current frame</param>
        void GetNextTarget(int frame)
        {
            if (BaseData.EnemyBases.Count() > TargetIndex + 1)
            {
                TargetIndex++;
                Target = BaseData.EnemyBases[TargetIndex].MineralLineBuildingLocation;
                return;
            }

            if (TargetIndex == 0)
            {
                TargetIndex++;
                Target = BaseData.EnemyBaseLocations[TargetIndex].MineralLineBuildingLocation;
                return;
            }

            TargetIndex = 0;
            if (BaseData.EnemyBases.Any())
            {
                Target = BaseData.EnemyBases[TargetIndex].MineralLineBuildingLocation;
                return;
            }
            Target = BaseData.EnemyBaseLocations[TargetIndex].MineralLineBuildingLocation;        
        }

        void GetTargetPath(Point2D target, int frame)
        {
            TargetIndex = 0;
            PathToEnemyMainData = HarassPathingService.GetHomeToEnemyBaseAirPath(target);
        }

        private bool Undetected(UnitCommander commander)
        {
            return CloakedOrCanCloak(commander) && !MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos);
        }

        bool CloakedOrCanCloak(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)) { return false; }
            return commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) || (commander.UnitCalculation.Unit.Energy > 50 && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEECLOAK));
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
                }
                else
                {
                    var kill = UnitCommanders.SelectMany(c => c.UnitCalculation.PreviousUnitCalculation.NearbyEnemies.Where(e => Vector2.DistanceSquared(c.UnitCalculation.Position, e.Position) < 50)).FirstOrDefault(e => e.Unit.Tag == tag);
                    if (kill != null && !SharkyUnitData.UndeadTypes.Contains((UnitTypes)kill.Unit.UnitType))
                    {
                        kills++;
                        Kills[tag] = kill;
                    }
                }
            }

            if (deaths > 0)
            {
                // TODO: chat for death, was or was not worth it
                Deaths += deaths;
            }
            if (kills > 0 || deaths > 0)
            {
                // TOOD: chat for kills, 10 kills, 25, 
                ReportResults();
            }
        }

        private void ReportResults()
        {
            Console.WriteLine($"{FrameToTimeConverter.GetTime(MacroData.Frame)} BansheeHarass Report: Deaths:{Deaths}, Kills:{Kills.Count()}");
            foreach (var killGroup in Kills.Values.GroupBy(k => k.Unit.UnitType))
            {
                Console.WriteLine($"{(UnitTypes)killGroup.Key}: {killGroup.Count()}");
            }
        }

        public override void PrintReport(int frame)
        {
            ReportResults();
            base.PrintReport(frame);
        }

        public override void Disable()
        {
            ReportResults();
            base.Disable();
        }
    }
}
