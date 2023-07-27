namespace Sharky.MicroTasks.Proxy
{
    public class WarpPrismElevatorTask : MicroTask
    {
        TargetingData TargetingData;
        AdvancedMicroController MicroController;
        WarpPrismMicroController WarpPrismMicroController;
        public IProxyLocationService ProxyLocationService { get; set; }
        MapDataService MapDataService;
        DebugService DebugService;
        UnitDataService UnitDataService;
        ActiveUnitData ActiveUnitData;
        ChatService ChatService;
        AreaService AreaService;
        TargetingService TargetingService;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        MicroTaskData MicroTaskData;
        EnemyData EnemyData;

        IBuildingPlacement ProtossBuildingPlacement;

        float lastFrameTime;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        Point2D DefensiveLocation { get; set; }
        Point2D LoadingLocation { get; set; }
        int LoadingLocationHeight { get; set; }
        Point2D DropLocation { get; set; }
        Point2D TargetLocation { get; set; }
        int DropLocationHeight { get; set; }
        float InsideBaseDistanceSquared { get; set; }
        int PickupRangeSquared { get; set; }
        List<Point2D> DropArea { get; set; }
        List<Point2D> AttackArea { get; set; }
        Point2D EnemyRampCenter { get; set; }
        int LastForceFieldFrame { get; set; }

        ArmySplitter DefenseArmySplitter;

        bool StartElevating { get; set; }
        public bool Completed { get; private set; }
        public bool AttackWithinArea { get; set; }
        public bool EndAfterMainBaseGone { get; set; }
        public bool CancelWhenPrismDies { get; set; }

        public WarpPrismElevatorTask(DefaultSharkyBot defaultSharkyBot, AdvancedMicroController microController, WarpPrismMicroController warpPrismMicroController, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            ProxyLocationService = defaultSharkyBot.ProxyLocationService;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            UnitDataService = defaultSharkyBot.UnitDataService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            AreaService = defaultSharkyBot.AreaService;
            ChatService = defaultSharkyBot.ChatService;
            TargetingService = defaultSharkyBot.TargetingService;
            ProtossBuildingPlacement = defaultSharkyBot.ProtossBuildingPlacement;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            EnemyData = defaultSharkyBot.EnemyData;

            MicroController = microController;
            WarpPrismMicroController = warpPrismMicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            PickupRangeSquared = 25;

            StartElevating = false;
            Completed = false;
            LastForceFieldFrame = 0;
            AttackWithinArea = true;
            EndAfterMainBaseGone = false;
            CancelWhenPrismDies = true;

            DefenseArmySplitter = new ArmySplitter(defaultSharkyBot);
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (!Enabled) { return; }
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
                            {
                                commander.Value.UnitRole = UnitRole.SaveEnergy;
                            }
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }

            var probeClaim = DesiredUnitsClaims.FirstOrDefault(c => c.UnitType == UnitTypes.PROTOSS_PROBE);
            if (probeClaim != null && probeClaim.Count > UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE))
            {
                var commander = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))).FirstOrDefault();

                if (commander != null)
                {
                    MicroTaskData[typeof(MiningTask).Name].StealUnit(commander);
                    commander.UnitRole = UnitRole.Proxy;
                    commander.Claimed = true;
                    UnitCommanders.Add(commander);
                    return;
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            SetLocations();

            var actions = new List<SC2APIProtocol.Action>();

            if (lastFrameTime > 5)
            {
                lastFrameTime = 0;
                return actions;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            CheckComplete();

            IEnumerable<UnitCommander> defenders = new List<UnitCommander>();

            var attackingEnemies = ActiveUnitData.EnemyUnits.Values.Where(e => e.FrameLastSeen > frame - 100 &&
                (e.NearbyEnemies.Any(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.UnitClassifications.Contains(UnitClassification.ProductionStructure) || u.UnitClassifications.Contains(UnitClassification.DefensiveStructure))) 
                && (e.NearbyEnemies.Count(b => b.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) >= e.NearbyAllies.Count(b => b.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))).Where(e => e.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE);

            if (attackingEnemies.Count() > 0)
            {
                var attackingEnemyVector = TargetingService.GetArmyPoint(attackingEnemies).ToVector2();

                if (attackingEnemies.Count() > 0)
                {
                    defenders = UnitCommanders.Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, attackingEnemyVector) < Vector2.DistanceSquared(c.UnitCalculation.Position, LoadingLocation.ToVector2()));
                    if (defenders.Any())
                    {
                        actions = DefenseArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.AttackPoint, defenders, true);
                    }
                }
            }

            var warpPrisms = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING).Where(c => !defenders.Contains(c));
            var attackers = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_WARPPRISM && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_WARPPRISMPHASING).Where(c => !defenders.Contains(c));
            var droppedAttackers = attackers.Where(c => AreaService.InArea(c.UnitCalculation.Unit.Pos, DropArea));
            var droppedSentries = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY);
            var droppedProbes = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE);
            var unDroppedAttackers = attackers.Where(c => !AreaService.InArea(c.UnitCalculation.Unit.Pos, DropArea));

            var readyForPickup = unDroppedAttackers.Where(c => !c.UnitCalculation.Loaded && Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(LoadingLocation.X, LoadingLocation.Y)) < 25);
            if (!StartElevating && readyForPickup.Any())
            {
                StartElevating = true;
            }

            if (!StartElevating && attackers.Any())
            {
                var leader = attackers.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
                if (leader == null)
                {
                    var immortal = attackers.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_IMMORTAL);
                    if (immortal != null)
                    {
                        immortal.UnitRole = UnitRole.Leader;
                        leader = attackers.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
                    }
                    else
                    {
                        leader = attackers.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
                    }
                }

                var leaders = attackers.Where(c => c.UnitRole == UnitRole.Leader);
                if (leaders.Any())
                {
                    actions.AddRange(MicroController.Retreat(leaders, LoadingLocation, null, frame));
                    actions.AddRange(MicroController.Support(UnitCommanders.Where(c => c.UnitRole != UnitRole.Leader), leaders, LoadingLocation, LoadingLocation, null, frame));
                }
                else
                {
                    actions.AddRange(MicroController.Attack(attackers, LoadingLocation, LoadingLocation, null, frame));
                }
            }
            else if (warpPrisms.Count() > 0)
            {
                foreach (var commander in warpPrisms)
                {
                    var action = OrderWarpPrism(commander, droppedAttackers, readyForPickup, unDroppedAttackers, frame);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }

                // move into the loading position
                var threatenedAttackers = unDroppedAttackers.Where(c => c.UnitCalculation.EnemiesThreateningDamage.Any());
                if (threatenedAttackers.Any())
                {
                    actions.AddRange(MicroController.Retreat(unDroppedAttackers, LoadingLocation, null, frame));
                }
                foreach (var commander in unDroppedAttackers.Where(c => !c.UnitCalculation.EnemiesThreateningDamage.Any()))
                {
                    var action = commander.Order(frame, Abilities.ATTACK, LoadingLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }                
                }
            }
            else
            {
                // wait for a warp prism
                var groupPoint = TargetingService.GetArmyPoint(unDroppedAttackers);
                actions.AddRange(MicroController.Retreat(unDroppedAttackers, DefensiveLocation, groupPoint, frame));
            }

            OrderSentries(frame, actions, droppedSentries);
            OrderProbes(frame, actions, droppedProbes);

            var mainAttackers = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_SENTRY && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_PROBE);


            if (AttackWithinArea)
            {
                actions.AddRange(MicroController.AttackWithinArea(mainAttackers, AttackArea, TargetLocation, DefensiveLocation, null, frame));
            }
            else
            {
                actions.AddRange(MicroController.Attack(mainAttackers, TargetLocation, DefensiveLocation, null, frame));
            }

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void OrderProbes(int frame, List<SC2APIProtocol.Action> actions, IEnumerable<UnitCommander> droppedProbes)
        {
            foreach (var commander in droppedProbes)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON) || (commander.LastAbility == Abilities.BUILD_PYLON && commander.LastOrderFrame + 20 > frame)) { continue; }

                if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON))
                {
                    if (MacroData.Minerals >= 100)
                    {
                        var placement = ProtossBuildingPlacement.FindPlacement(DropLocation, UnitTypes.PROTOSS_PYLON, 1, true, 50, true);
                        if (placement != null)
                        {
                            var action = commander.Order(frame, Abilities.BUILD_PYLON, placement);
                            if (action != null)
                            {
                                actions.AddRange(action);
                                continue;
                            }
                        }
                    }
                }

                if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    actions.AddRange(MicroController.Retreat(new List<UnitCommander> { commander }, DropLocation, null, frame));
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
        }

        private void OrderSentries(int frame, List<SC2APIProtocol.Action> actions, IEnumerable<UnitCommander> droppedSentries)
        {
            if (EnemyRampCenter == null)
            {
                actions.AddRange(MicroController.Attack(droppedSentries, TargetLocation, DefensiveLocation, null, frame));
                return;
            }
            var forceField = ActiveUnitData.NeutralUnits.Values.FirstOrDefault(u => u.Unit.UnitType == (uint)UnitTypes.NEUTRAL_FORCEFIELD && Vector2.DistanceSquared(EnemyRampCenter.ToVector2(), u.Position) < 1);
            var someoneCastingFF = droppedSentries.Any(commander => commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD) || (commander.LastAbility == Abilities.EFFECT_FORCEFIELD && commander.LastOrderFrame + 20 > frame));
            foreach (var commander in droppedSentries)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD) || (commander.LastAbility == Abilities.EFFECT_FORCEFIELD && commander.LastOrderFrame + 20 > frame)) { continue; }

                if (forceField == null  && commander.UnitCalculation.Unit.Energy >= 50 && frame - LastForceFieldFrame > 20 && !someoneCastingFF)
                {
                    LastForceFieldFrame = frame;
                    var ffAction = commander.Order(frame, Abilities.EFFECT_FORCEFIELD, EnemyRampCenter);
                    if (ffAction != null)
                    {
                        actions.AddRange(ffAction);
                    }
                    continue;
                }

                if (commander.UnitCalculation.EnemiesThreateningDamage.Any() || commander.UnitCalculation.Unit.BuffIds.Any(b => b == (uint)Buffs.LOCKON))
                {
                    actions.AddRange(MicroController.Retreat(new List<UnitCommander> { commander }, DropLocation, null, frame));
                }
                else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, EnemyRampCenter.ToVector2()) > 81)
                {
                    var action = commander.Order(frame, Abilities.MOVE, EnemyRampCenter);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
        }

        private void CheckComplete()
        {
            if (MapDataService.SelfVisible(TargetLocation))
            {
                if (!ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(TargetLocation.X, TargetLocation.Y), e.Value.Position) < 100) || (EndAfterMainBaseGone && !ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && Vector2.DistanceSquared(new Vector2(TargetLocation.X, TargetLocation.Y), e.Value.Position) < 100)))
                {
                    Disable();
                    Completed = true;
                    ChatService.SendChatType("WarpPrismElevatorTask-TaskCompleted");
                }
            }
        }

        private List<SC2APIProtocol.Action> OrderWarpPrism(UnitCommander warpPrism, IEnumerable<UnitCommander> droppedAttackers, IEnumerable<UnitCommander> readyForPickup, IEnumerable<UnitCommander> unDroppedAttackers, int frame)
        {
            if (!StartElevating || warpPrism.UnitCalculation.Unit.BuffIds.Any(b => b == (uint)Buffs.LOCKON))
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DefensiveLocation, null, frame, out action, unDroppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            foreach (var pickup in readyForPickup.OrderByDescending(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING).ThenByDescending(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_IMMORTAL).ThenByDescending(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY))
            {
                if (warpPrism.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
                {
                    return warpPrism.Order(frame, Abilities.MORPH_WARPPRISMTRANSPORTMODE);
                }

                if (warpPrism.UnitCalculation.Unit.CargoSpaceMax - warpPrism.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)pickup.UnitCalculation.Unit.UnitType) && !warpPrism.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM))
                {
                    if (Vector2.DistanceSquared(warpPrism.UnitCalculation.Position, pickup.UnitCalculation.Position) < PickupRangeSquared)
                    {
                        return warpPrism.Order(frame, Abilities.LOAD, null, pickup.UnitCalculation.Unit.Tag);
                    }
                    else
                    {
                        return warpPrism.Order(frame, Abilities.MOVE, LoadingLocation);
                    }
                }
            }

            if (warpPrism.UnitCalculation.Unit.Passengers.Count() > 0)
            {
                if (AreaService.InArea(warpPrism.UnitCalculation.Unit.Pos, DropArea))
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, warpPrism.UnitCalculation.Unit.Tag);
                }
                else
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, DropLocation);
                }
            }

            if (warpPrism.UnitCalculation.EnemiesThreateningDamage.Any() || warpPrism.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER))
            {
                return WarpPrismMicroController.Retreat(warpPrism, DefensiveLocation, null, frame);
            }

            if (droppedAttackers.Count() > 0)
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DropLocation, null, frame, out action, droppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            if (unDroppedAttackers.Count() > 0)
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DefensiveLocation, null, frame, out action, unDroppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            return warpPrism.Order(frame, Abilities.MOVE, DefensiveLocation);
        }

        private void SetLocations()
        {
            if (DefensiveLocation == null)
            {
                DefensiveLocation = ProxyLocationService.GetCliffProxyLocation();
                TargetLocation = TargetingData.EnemyMainBasePoint;
                DropArea = AreaService.GetTargetArea(TargetLocation);

                var angle = Math.Atan2(TargetLocation.Y - DefensiveLocation.Y, DefensiveLocation.X - TargetLocation.X);
                var x = -6 * Math.Cos(angle);
                var y = -6 * Math.Sin(angle);
                LoadingLocation = DefensiveLocation;
                LoadingLocationHeight = MapDataService.MapHeight(LoadingLocation);

                x = -7 * Math.Cos(angle);
                y = -7 * Math.Sin(angle);
                var loadingVector = new Vector2(LoadingLocation.X + (float)x, LoadingLocation.Y - (float)y);

                var dropVector = DropArea.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), loadingVector)).First();
                x = -2 * Math.Cos(angle);
                y = -2 * Math.Sin(angle);
                DropLocation = new Point2D { X = dropVector.X + (float)x, Y = dropVector.Y - (float)y };
                DropLocationHeight = MapDataService.MapHeight(DropLocation);

                InsideBaseDistanceSquared = Vector2.DistanceSquared(new Vector2(LoadingLocation.X, LoadingLocation.Y), new Vector2(TargetLocation.X, TargetLocation.Y));

                var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.EnemyMainBasePoint.X && b.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y);
                if (wallData?.RampCenter != null)
                {
                    EnemyRampCenter = wallData.RampCenter;
                    var distanceSquaredToAvoid = 50;
                    if (EnemyData.EnemyRace == Race.Terran)
                    {
                        distanceSquaredToAvoid = 144;
                    }
                    AttackArea = DropArea; //.Where(p => Vector2.DistanceSquared(p.ToVector2(), EnemyRampCenter.ToVector2()) > distanceSquaredToAvoid).ToList();
                }
                else
                {
                    AttackArea = DropArea;
                }
            }
            DebugService.DrawSphere(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawLine(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 16 }, new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 0 }, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 255, B = 0 });
            DebugService.DrawLine(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 16 }, new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 0 }, new Color { R = 0, G = 255, B = 0 });
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                if (CancelWhenPrismDies && UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == tag && (c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)))
                {
                    Disable();
                    Console.WriteLine($"WarpPrismElevatorTask: Ending because Warp Prism died");
                    return;
                }
                Deaths += UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
