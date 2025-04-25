namespace Sharky.MicroTasks
{
    public class WorkerScoutGasStealTask : MicroTask
    {
        public bool StealGas { get; set; }
        public bool BlockExpansion { get; set; }
        public bool BlockLiftedExpansion { get; set; }
        public bool HidePylonInBase { get; set; }
        public bool BlockWall { get; set; }
        public bool BlockAddons { get; set; }
        public bool RecallProbe { get; set; }
        public bool ChaseWorkers { get; set; }
        public bool BodyBlockExpansion { get; set; }
        public bool BodyBlockUntilDeath { get; set; }
        public bool PrioritizeExpansion { get; set; }
        public bool OnlyExpansions { get; set; }

        public bool ScoutEntireAreaBeforeAttacking { get; set; }
        public bool OnlyBlockOnce { get; set; }
        protected bool BlockedOnce = false;

        protected SharkyUnitData SharkyUnitData;
        protected TargetingData TargetingData;
        protected MacroData MacroData;
        protected MapDataService MapDataService;
        protected BaseData BaseData;
        protected MapData MapData;
        protected EnemyData EnemyData;
        protected AreaService AreaService;
        protected BuildingService BuildingService;
        protected UnitCountService UnitCountService;
        protected ActiveUnitData ActiveUnitData;
        protected SharkyOptions SharkyOptions;
        protected IBuildingBuilder BuildingBuilder;
        protected CameraManager CameraManager;
        MicroTaskData MicroTaskData;

        protected MineralWalker MineralWalker;

        protected List<Point2D> ScoutPoints;
        protected List<Point2D> EnemyMainArea;
        protected List<Point2D> DangerArea;

        CollisionCalculator CollisionCalculator;

        protected IndividualMicroController IndividualMicroController;
        protected IPathFinder PathFinder;

        protected bool started { get; set; }

        UnitCalculation Pylon { get; set; }

        List<Vector2> Path = new List<Vector2>();
        int PathFrame = 0;


        public WorkerScoutGasStealTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IndividualMicroController individualMicroController)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
            AreaService = defaultSharkyBot.AreaService;
            BuildingService = defaultSharkyBot.BuildingService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            MapData = defaultSharkyBot.MapData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MineralWalker = defaultSharkyBot.MineralWalker;
            BuildingBuilder = defaultSharkyBot.BuildingBuilder;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            CameraManager = defaultSharkyBot.CameraManager;
            PathFinder = defaultSharkyBot.SharkyWorkerScoutPathFinder;
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;

            UnitCommanders = new List<UnitCommander>();

            Enabled = enabled;
            Priority = priority;

            StealGas = true;
            BlockExpansion = false;
            BlockLiftedExpansion = false;
            HidePylonInBase = false;
            BlockWall = false;
            BlockAddons = false;
            RecallProbe = false;
            OnlyBlockOnce = false;
            ScoutEntireAreaBeforeAttacking = true;
            ChaseWorkers = false;
            BodyBlockExpansion = false;
            PrioritizeExpansion = false;
            IndividualMicroController = individualMicroController;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    return;
                }

                var workers = commanders.Where(commander => commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker));
                var scouter = workers.FirstOrDefault(commander => !commander.Value.Claimed);
                if (scouter.Value == null)
                {
                    var available = workers.Where(commander => !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)));
                    var finishedBuilder = available.FirstOrDefault(commander => commander.Value.UnitRole == UnitRole.Build && commander.Value.UnitCalculation.Unit.Orders.Any(o => !o.HasTargetUnitTag));
                    if (finishedBuilder.Value != null)
                    {
                        var pos = finishedBuilder.Value.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.TargetWorldSpacePos != null);
                        if (pos != null)
                        {
                            var match = finishedBuilder.Value.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && a.Unit.Pos.X == pos.TargetWorldSpacePos.X && a.Unit.Pos.Y == pos.TargetWorldSpacePos.Y);
                            if (match)
                            {
                                MicroTaskData[typeof(MiningTask).Name].StealUnit(finishedBuilder.Value);
                                finishedBuilder.Value.Claimed = true;
                                finishedBuilder.Value.UnitRole = UnitRole.Scout;
                                UnitCommanders.Add(finishedBuilder.Value);
                                started = true;
                                return;
                            }
                        }
                    }
                    if (scouter.Value == null)
                    {
                        scouter = available.FirstOrDefault();
                    }
                }

                if (scouter.Value != null)
                {
                    scouter.Value.Claimed = true;
                    scouter.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(scouter.Value);
                    started = true;
                    return;
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            var positions = ActiveUnitData.Commanders.Values.Where(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON).Select(p => p.UnitCalculation.Position);

            if (ScoutPoints == null)
            {
                EnemyMainArea = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);
                DangerArea = EnemyMainArea.Where(p => Vector2.DistanceSquared(p.ToVector2(), TargetingData.EnemyMainBasePoint.ToVector2()) < 100 && Vector2.DistanceSquared(p.ToVector2(), BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation.ToVector2()) < 100).ToList();
                EnemyMainArea.RemoveAll(p => Vector2.DistanceSquared(p.ToVector2(), TargetingData.EnemyMainBasePoint.ToVector2()) < 100 && Vector2.DistanceSquared(p.ToVector2(), BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation.ToVector2()) < 100);
                ScoutPoints = new List<Point2D>();
                ScoutPoints.AddRange(EnemyMainArea);
                ScoutPoints.Add(BaseData.EnemyBaseLocations.Skip(1).First().Location);
            }

            var mainVector = TargetingData.EnemyMainBasePoint.ToVector2();

            bool disable = false;

            foreach (var commander in UnitCommanders)
            {
                if (BlockExpansion && Vector2.Distance(commander.UnitCalculation.Position, mainVector) < 50)
                {
                    foreach (var pylon in commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON))
                    {
                        ActiveUnitData.Commanders[pylon.Unit.Tag].UnitRole = UnitRole.BlockExpansion;
                    }
                }

                if (OnlyBlockOnce && BlockedOnce)
                {
                    var pylon = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
                    if (pylon != null)
                    {
                        Pylon = pylon;
                    }
                    if (Pylon != null && Pylon.Unit.Shield < 25 && Pylon.Unit.BuildProgress > .95f && Pylon.Unit.BuildProgress < 1)
                    {
                        var pylonCommander = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.Tag == Pylon.Unit.Tag);
                        if (pylonCommander != null)
                        {
                            commands.AddRange(pylonCommander.Order(frame, Abilities.CANCEL));
                        }
                    }
                }

                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR) || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON))
                {
                    continue;
                }

                var expansion = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                if (BaseData.EnemyBases.Count > 1)
                {
                    expansion = BaseData.EnemyBaseLocations.FirstOrDefault(l => !BaseData.EnemyBases.Any(b => b.Location.X == l.Location.X && b.Location.Y == l.Location.Y));
                    if (expansion == null)
                    {
                        expansion = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                    }
                }

                if (BodyBlockExpansion && BodyBlockUntilDeath && CanBodyBlockExpansion(commander, expansion))
                {
                    if (CanBodyBlockExpansion(commander, expansion))
                    {
                        BodyBlockExpansionWithProbe(frame, commands, commander, expansion);
                        continue;
                    }
                }

                if (commander.UnitCalculation.Unit.ShieldMax > 5 && (commander.UnitCalculation.Unit.Shield < 5 || (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.HasFlag(UnitClassification.Worker)) > 0)))
                {
                    if (MineralWalker.MineralWalkNoWhere(commander, frame, out List<SC2Action> mineralWalk))
                    {
                        commands.AddRange(mineralWalk);
                        continue;
                    }
                }

                if (TryRecallProbe(commander, frame, commands, disable))
                {
                    continue;
                }

                if (TryBodyBlockExpansion(commander, frame, commands, expansion))
                {
                    continue;
                }

                if (StealGas && MacroData.Minerals >= 75 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    if (!BaseData.EnemyBases.Any(enemyBase => enemyBase.VespeneGeysers.Any(g => g.Alliance == Alliance.Enemy)) && MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) > 0)
                    {
                        foreach (var enemyBase in BaseData.EnemyBases.Where(enemyBase => enemyBase.ResourceCenter != null && enemyBase.ResourceCenter.BuildProgress == 1))
                        {
                            foreach (var gas in enemyBase.VespeneGeysers.Where(g => g.Alliance == Alliance.Neutral && MapDataService.SelfVisible(g.Pos)))
                            {
                                var gasVector = gas.Pos.ToVector2();
                                if (Vector2.DistanceSquared(gasVector, commander.UnitCalculation.Position) < 400 && commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) > 5)
                                {
                                    if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker) && CollisionCalculator.Collides(e.Position, 1, commander.UnitCalculation.Position, gasVector)) < 3)
                                    {
                                        var gasSteal = commander.Order(frame, Abilities.BUILD_ASSIMILATOR, null, gas.Tag);
                                        if (gasSteal != null)
                                        {
                                            CameraManager.SetCamera(gas.Pos);
                                            commands.AddRange(gasSteal);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (MacroData.Minerals >= 100 && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                {
                    var enemyBase = BaseData.EnemyBaseLocations.FirstOrDefault();

                    if (BlockAddons && EnemyData.EnemyRace == Race.Terran && !(BlockedOnce && OnlyBlockOnce))
                    {
                        var buildingWithoutAddon = commander.UnitCalculation.NearbyEnemies.Where(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || e.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || e.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT) && !e.Unit.HasAddOnTag && BuildingBuilder.HasRoomForAddon(e.Unit)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (buildingWithoutAddon != null)
                        {
                            var point = new Point2D { X = buildingWithoutAddon.Unit.Pos.X + 2.5f, Y = buildingWithoutAddon.Unit.Pos.Y - .5f };
                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                            if (wallBlock != null)
                            {
                                CameraManager.SetCamera(point);
                                commands.AddRange(wallBlock);
                                BlockedOnce = true;
                                continue;
                            }
                        }
                    }

                    if (TryBlockWall(enemyBase, commands, commander, frame))
                    {
                        continue;
                    }

                    if (expansion != null)
                    {
                        if (BlockExpansion || (BlockLiftedExpansion && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) > 1))
                        {
                            var expansionVector = new Vector2(expansion.Location.X, expansion.Location.Y);
                            if (Vector2.DistanceSquared(expansionVector, commander.UnitCalculation.Position) < 225)
                            {
                                if (!commander.UnitCalculation.NearbyAllies.Any(a => Vector2.DistanceSquared(expansionVector, a.Position) < 9) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ResourceCenter) && !e.Unit.IsFlying && Vector2.DistanceSquared(expansionVector, e.Position) < 2))
                                {
                                    var expansionBlock = commander.Order(frame, Abilities.BUILD_PYLON, expansion.Location);
                                    if (expansionBlock != null)
                                    {
                                        CameraManager.SetCamera(expansion.Location);
                                        commands.AddRange(expansionBlock);
                                        continue;
                                    }
                                }
                            }
                        }
                    }

                    if (HidePylonInBase)
                    {
                        var hideLocation = EnemyMainArea.Where(p => MapDataService.SelfVisible(p) && !MapDataService.InEnemyVision(p)).OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), mainVector)).FirstOrDefault();
                        if (hideLocation != null)
                        {
                            var hidenPylonOrder = commander.Order(frame, Abilities.BUILD_PYLON, hideLocation);
                            if (hidenPylonOrder != null)
                            {
                                CameraManager.SetCamera(hideLocation);
                                commands.AddRange(hidenPylonOrder);
                                continue;
                            }
                        }
                    }
                }

                if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
                {
                    var wallData = MapData.WallData.FirstOrDefault(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y);
                    if (wallData != null)
                    {
                        var production = wallData?.Production?.FirstOrDefault();
                        if (production != null)
                        {
                            var vector = new Vector2(production.X, production.Y);
                            if (Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 25)
                            {
                                if (wallData.Depots != null && wallData.Depots.All(d => commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT && e.Unit.Pos.X == d.X && e.Unit.Pos.Y == d.Y)))
                                {
                                    var prodBuilding = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !e.Unit.IsFlying && e.Unit.Pos.X == production.X && e.Unit.Pos.Y == production.Y);
                                    if (prodBuilding != null)
                                    {
                                        commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: prodBuilding.Unit.Tag));
                                        continue;
                                    }
                                    else
                                    {
                                        if (MacroData.Minerals >= 100 && Vector2.DistanceSquared(vector, commander.UnitCalculation.Position) < 4 && !commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && a.Unit.Pos.X == production.X && a.Unit.Pos.Y == production.Y))
                                        {
                                            CameraManager.SetCamera(production);
                                            commands.AddRange(commander.Order(frame, Abilities.BUILD_PYLON, new Point2D { X = production.X, Y = production.Y }));
                                            continue;
                                        }
                                        else
                                        {
                                            commands.AddRange(commander.Order(frame, Abilities.MOVE, new Point2D { X = production.X, Y = production.Y }));
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var liftedBuilding = commander.UnitCalculation.NearbyEnemies.Where(e => e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.Unit.IsFlying).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
                    if (liftedBuilding != null)
                    {
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, new Point2D { X = liftedBuilding.Position.X, Y = liftedBuilding.Position.Y }));
                        continue;
                    }

                    // if worker building building nearby, attack it
                    if (commander.UnitCalculation.Unit.Shield > 5)
                    {
                        var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e =>
                            e.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV &&
                                e.NearbyAllies.Any(a => a.Unit.BuildProgress < 1 && Vector2.DistanceSquared(a.Position, e.Position) < ((e.Unit.Radius + a.Unit.Radius + 1) * (e.Unit.Radius + a.Unit.Radius + 1)))
                            );
                        if (enemy != null)
                        {
                            CameraManager.SetCamera(enemy.Position);
                            if (enemy.FrameLastSeen == frame)
                            {
                                commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: enemy.Unit.Tag));
                            }
                            else
                            {
                                commands.AddRange(commander.Order(frame, Abilities.ATTACK, enemy.Position.ToPoint2D()));
                            }
                            continue;
                        }
                    }
                }

                if (frame % 50 == 0)
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                }

                if (PrioritizeExpansion)
                {
                    if (MapDataService.LastFrameVisibility(expansion.Location) + (15 * SharkyOptions.FramesPerSecond) < frame)
                    {
                        if (Vector2.Distance(commander.UnitCalculation.Position, expansion.Location.ToVector2()) < 5)
                        {
                            BodyBlockExpansionWithProbe(frame, commands, commander, expansion);
                            continue;
                        }
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, expansion.Location));
                        continue;
                    }
                }
                if (OnlyExpansions)
                {
                    if (BaseData.EnemyBases.Count < 2)
                    {
                        if (MapDataService.LastFrameVisibility(expansion.Location) + (15 * SharkyOptions.FramesPerSecond) < frame)
                        {
                            if (Vector2.Distance(commander.UnitCalculation.Position, expansion.Location.ToVector2()) < 5)
                            {
                                BodyBlockExpansionWithProbe(frame, commands, commander, expansion);
                                continue;
                            }
                            commands.AddRange(commander.Order(frame, Abilities.MOVE, expansion.Location));
                            continue;
                        }
                    }
                    else
                    {
                        var expansions = BaseData.EnemyBaseLocations.Where(l => !BaseData.EnemyBases.Any(b => b.Location.X == l.Location.X && b.Location.Y == l.Location.Y)).Take(2);
                        bool tookAction = false;
                        foreach (var extraExpansions in expansions)
                        {
                            if (MapDataService.LastFrameVisibility(extraExpansions.Location) + (15 * SharkyOptions.FramesPerSecond) < frame || commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker) && Vector2.Distance(e.Position, extraExpansions.Location.ToVector2()) < 5))
                            {
                                if (Vector2.Distance(commander.UnitCalculation.Position, extraExpansions.Location.ToVector2()) < 5)
                                {
                                    BodyBlockExpansionWithProbe(frame, commands, commander, extraExpansions);
                                    tookAction = true;
                                    continue;
                                }
                                commands.AddRange(commander.Order(frame, Abilities.MOVE, extraExpansions.Location));
                                tookAction = true;
                                continue;
                            }
                        }
                        if (tookAction) { continue; }
                    }
                    if ((commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && e.FrameLastSeen == frame && Vector2.Distance(commander.UnitCalculation.Position, e.Position) < 15)) ||
                        commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
                    {
                        if (MineralWalker.MineralWalkNoWhere(commander, frame, out List<SC2Action> mineralWalk))
                        {
                            commands.AddRange(mineralWalk);
                            continue;
                        }
                    }
                }

                if (ChaseWorkers)
                {
                    var closestWorker = commander.UnitCalculation.NearbyEnemies.Where(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (closestWorker != null)
                    {
                        var probes = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && e.FrameLastSeen == frame);
                        var pylons = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
                        if (probes.Count() > 1 && pylons.Count() == 1)
                        {
                            var pylon = pylons.OrderBy(e => Vector2.Distance(e.Position, commander.UnitCalculation.Position)).FirstOrDefault(e => !e.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && a.Unit.BuildProgress == 1));
                            if (pylon != null)
                            {
                                var closestProbe = probes.OrderBy(e => Vector2.Distance(e.Position, pylon.Position)).FirstOrDefault();
                                commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: closestProbe.Unit.Tag));
                                continue;
                            }
                        }

                        if (closestWorker.FrameLastSeen == frame)
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: closestWorker.Unit.Tag));
                        }
                        else
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, closestWorker.Position.ToPoint2D()));
                        }
                        continue;
                    }
                }

                var points = ScoutPoints.Where(p => Vector2.DistanceSquared(p.ToVector2(), commander.UnitCalculation.Position) < 170 || Vector2.DistanceSquared(p.ToVector2(), mainVector) < 170).OrderBy(p => MapDataService.LastFrameVisibility(p)).ThenBy(p => Vector2.DistanceSquared(p.ToVector2(), mainVector)).ThenBy(p => Vector2.DistanceSquared(commander.UnitCalculation.Position, p.ToVector2()));
                var navpoint = points.FirstOrDefault(p => !MapDataService.PathBlocked(p.ToVector2()));
                if (MapDataService.LastFrameVisibility(navpoint) > 100 && !ScoutPoints.All(p => MapDataService.LastFrameVisibility(p) > 100 || MapDataService.PathBlocked(p.ToVector2())))
                {
                    navpoint = ScoutPoints.OrderBy(p => MapDataService.LastFrameVisibility(p)).ThenBy(p => Vector2.DistanceSquared(p.ToVector2(), mainVector)).ThenBy(p => Vector2.DistanceSquared(commander.UnitCalculation.Position, p.ToVector2())).FirstOrDefault();
                }
                if (EnemyData.EnemyRace == Race.Zerg)
                {
                    var baseLocation = BaseData.EnemyBaseLocations.FirstOrDefault();
                    if (baseLocation != null)
                    {
                        if (MapDataService.LastFrameAlliesTouched(baseLocation.BehindMineralLineLocation) < 1 && MapDataService.MapHeight(commander.UnitCalculation.Position) == MapDataService.MapHeight(baseLocation.BehindMineralLineLocation) && Vector2.DistanceSquared(baseLocation.BehindMineralLineLocation.ToVector2(), commander.UnitCalculation.Position) < 400)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker) && CollisionCalculator.Collides(e.Position, 2, commander.UnitCalculation.Position, baseLocation.BehindMineralLineLocation.ToVector2())) > 1)
                            {
                                if (PathFrame + 20 < frame)
                                {
                                    PathFrame = frame;
                                    Path = PathFinder.GetSafeGroundPath(commander.UnitCalculation.Position.X, commander.UnitCalculation.Position.Y, baseLocation.BehindMineralLineLocation.X, baseLocation.BehindMineralLineLocation.Y, frame);
                                }
                                if (Path.Count() < 3)
                                {
                                    commands.AddRange(commander.Order(frame, Abilities.MOVE, baseLocation.BehindMineralLineLocation));
                                    return commands;
                                }
                                int index = 0;
                                foreach (var point in Path)
                                {
                                    index++;
                                    if (Vector2.DistanceSquared(point, commander.UnitCalculation.Position) > 4)
                                    {
                                        if (index >= Path.Count)
                                        {
                                            PathFrame = 0;
                                        }
                                        commands.AddRange(commander.Order(frame, Abilities.MOVE, point.ToPoint2D()));
                                        return commands;
                                    }
                                }
                            }
                            commands.AddRange(commander.Order(frame, Abilities.MOVE, baseLocation.BehindMineralLineLocation));
                            return commands;
                        }
                    }
                }
                if (navpoint == null)
                {
                    navpoint = ScoutPoints.OrderBy(p => MapDataService.LastFrameVisibility(p)).ThenBy(p => Vector2.DistanceSquared(p.ToVector2(), mainVector)).ThenBy(p => Vector2.DistanceSquared(commander.UnitCalculation.Position, p.ToVector2())).FirstOrDefault();
                }
                if (navpoint != null)
                {
                    if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.Unit.WeaponCooldown < 2)
                    {
                        var target = commander.UnitCalculation.EnemiesInRange.Where(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)).OrderBy(e => e.SimulatedHitpoints).ThenBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (target != null)
                        {
                            if (target.FrameLastSeen == frame)
                            {
                                commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: target.Unit.Tag));
                            }
                            else
                            {
                                commands.AddRange(commander.Order(frame, Abilities.ATTACK, target.Position.ToPoint2D()));
                            }
                            return commands;
                        }
                    }

                    if (Attack(commander, frame, commands, navpoint))
                    {
                        return commands;
                    }

                    if (Vector2.DistanceSquared(navpoint.ToVector2(), commander.UnitCalculation.Position) < 400 && MapDataService.MapHeight(commander.UnitCalculation.Position) == MapDataService.MapHeight(navpoint.ToVector2()) && commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker) && CollisionCalculator.Collides(e.Position, 1, commander.UnitCalculation.Position, navpoint.ToVector2())) > 1)
                    {
                        if (PathFrame + 20 < frame)
                        {
                            PathFrame = frame;
                            Path = PathFinder.GetSafeGroundPath(commander.UnitCalculation.Position.X, commander.UnitCalculation.Position.Y, navpoint.X, navpoint.Y, frame);
                        }
                        if (Path.Count() > 2)
                        {
                            var index = 0;
                            foreach (var point in Path)
                            {
                                index++;
                                if (Vector2.DistanceSquared(point, commander.UnitCalculation.Position) > 4)
                                {
                                    if (index >= Path.Count)
                                    {
                                        PathFrame = 0;
                                    }
                                    commands.AddRange(commander.Order(frame, Abilities.MOVE, point.ToPoint2D()));
                                    return commands;
                                }
                            }
                        }
                    }
                    
                    if (MapDataService.LastFrameVisibility(navpoint) > 0)
                    {
                        var action = commander.Order(frame, Abilities.MOVE, navpoint);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        List<SC2Action> action;
                        if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax)
                        {
                            IndividualMicroController.NavigateToTarget(commander, navpoint, null, null, Formation.Normal, frame, out action);
                        }
                        else
                        {
                            action = IndividualMicroController.NavigateToPoint(commander, navpoint, navpoint, null, frame);
                        }

                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
            }

            if (!UnitCommanders.Any() && started)
            {
                disable = true;
            }

            if (disable)
            {
                if (Pylon != null)
                {
                    var pylonCommander = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.Tag == Pylon.Unit.Tag);
                    if (pylonCommander != null)
                    {
                        commands.AddRange(pylonCommander.Order(frame, Abilities.CANCEL));
                    }
                }

                Disable();
            }

            return commands;
        }

        protected virtual bool TryBodyBlockExpansion(UnitCommander commander, int frame, List<SC2Action> commands, BaseLocation expansion)
        {
            if (BodyBlockExpansion && CanBodyBlockExpansion(commander, expansion))
            {
                if (CanBodyBlockExpansion(commander, expansion))
                {
                    BodyBlockExpansionWithProbe(frame, commands, commander, expansion);
                    return true;
                }
            }
            return false;
        }

        protected void BodyBlockExpansionWithProbe(int frame, List<SC2Action> commands, UnitCommander commander, BaseLocation expansion)
        {
            var zergling = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            var drone = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (!commander.UnitCalculation.EnemiesInRangeOf.Any() && zergling != null && drone != null)
            {
                var zd = Vector2.Distance(zergling.Position, commander.UnitCalculation.Position);
                var dd = Vector2.Distance(drone.Position, commander.UnitCalculation.Position);
                var lingToDrone = Vector2.Distance(drone.Position, zergling.Position);
                if (dd < zd && lingToDrone < zd) // safer just standing still, ling will block expansion trying to get to probe
                {
                    if (Vector2.Distance(commander.UnitCalculation.Position, expansion.Location.ToVector2()) > 2f)
                    {
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, expansion.Location));
                    }
                    else
                    {
                        var away = Vector2.Lerp(commander.UnitCalculation.Position, zergling.Position, -1);
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, away.ToPoint2D()));
                    }
                }
            }

            var angle = CalculateStartAngle(expansion.Location.ToVector2(), commander.UnitCalculation.Position);
            var clockWise = true;
            if (commander.UnitCalculation.Unit.Facing < angle)
            {
                clockWise = false;
            }
            var radius = 2.75f;
            if (EnemyData.EnemyRace == Race.Protoss)
            {
                radius = 2f;
            }
            var nextPoint = CalculateNextCirclePoint(expansion.Location.ToVector2(), radius, 12, angle, clockWise);
            commands.AddRange(commander.Order(frame, Abilities.MOVE, nextPoint.ToPoint2D()));
        }

        protected bool CanBodyBlockExpansion(UnitCommander commander, BaseLocation expansion)
        {
            if (expansion == null) { return false; }
            return !BaseData.EnemyBases.Any(b => b.Location.X == expansion.Location.X && b.Location.Y == expansion.Location.Y) && (BodyBlockUntilDeath || Vector2.Distance(commander.UnitCalculation.Position, expansion.Location.ToVector2()) < 15);
        }

        protected virtual bool Attack(UnitCommander commander, int frame, List<SC2APIProtocol.Action> commands, Point2D navpoint)
        {
            if (commander.UnitCalculation.Unit.Shield > 15 && commander.UnitCalculation.NearbyEnemies.Any() && (commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)) || (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ResourceCenter) && e.Unit.BuildProgress == 1) && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))))
            {
                if (!ScoutEntireAreaBeforeAttacking || ScoutPoints.All(p => MapDataService.LastFrameVisibility(p) > 100 || MapDataService.PathBlocked(p.ToVector2())))
                {
                    commands.AddRange(commander.Order(frame, Abilities.ATTACK, navpoint));
                    return true;
                }
            }

            return false;
        }

        public override void Disable()
        {
            if (Pylon != null)
            {
                var pylonCommander = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.Tag == Pylon.Unit.Tag);
                if (pylonCommander != null)
                {
                    pylonCommander.UnitRole = UnitRole.Die;
                }
            }

            base.Disable();
        }

        private bool TryRecallProbe(UnitCommander commander, int frame, List<SC2APIProtocol.Action> commands, bool disable)
        {
            if (!RecallProbe) { return false; }

            if (frame > 1.9 * 60 * SharkyOptions.FramesPerSecond)
            {
                if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)) == 1 && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ProductionStructure)))
                {
                    var nexus = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1 && c.UnitCalculation.Unit.Energy >= 50);
                    if (nexus != null)
                    {
                        var baseLocation = BaseData.SelfBases.FirstOrDefault(b => b.ResourceCenter != null && b.ResourceCenter.Tag == nexus.UnitCalculation.Unit.Tag);
                        if (baseLocation != null)
                        {
                            var angle = Math.Atan2(baseLocation.Location.Y - baseLocation.MineralLineLocation.Y, baseLocation.MineralLineLocation.X - baseLocation.Location.X);
                            var recallPoint = new Point2D { X = commander.UnitCalculation.Position.X + (float)(-2 * Math.Cos(angle)), Y = commander.UnitCalculation.Position.Y - (float)(-2 * Math.Sin(angle)) };
                            var recall = nexus.Order(frame, Abilities.NEXUSMASSRECALL, recallPoint);
                            if (recall != null)
                            {
                                commands.AddRange(recall);
                                commands.AddRange(commander.Order(frame, Abilities.MOVE, commander.UnitCalculation.Position.ToPoint2D()));
                                disable = true;
                                return true;
                            }
                        }
                    }
                    if (disable) { return true; }
                }
            }

            if (frame > 1.75 * 60 * SharkyOptions.FramesPerSecond)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && e.Unit.IsActive))
                {
                    var scout = IndividualMicroController.Scout(commander, BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation, TargetingData.ForwardDefensePoint, frame, false, true);
                    if (scout != null)
                    {
                        commands.AddRange(scout);
                    }
                    return true;
                }
            }
            return false;
        }

        protected virtual bool TryBlockWall(BaseLocation enemyBase, List<SC2APIProtocol.Action> commands, UnitCommander commander, int frame)
        {
            if (!BlockWall || enemyBase == null || MapData.WallData == null) { return false; }

            var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == enemyBase.Location.X && b.BasePosition.Y == enemyBase.Location.Y);
            if (wallData == null) { return false; }

            if (Vector2.DistanceSquared(enemyBase.Location.ToVector2(), commander.UnitCalculation.Position) >= 225) { return false; }

            var alreadyBlocked = wallData.Depots != null && ActiveUnitData.SelfUnits.Any(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Depots.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), a.Value.Position) < 4));
            if (!alreadyBlocked)
            {
                alreadyBlocked = wallData.Production != null && ActiveUnitData.SelfUnits.Any(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Production.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), a.Value.Position) < 4));
            }
            if (alreadyBlocked) { return false; }

            if (wallData.Depots != null)
            {
                foreach (var point in wallData.Depots)
                {
                    if (!BuildingService.Blocked(point.X, point.Y, 1, -.5f))
                    {
                        var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                        if (wallBlock != null)
                        {
                            commands.AddRange(wallBlock);
                            return true;
                        }
                    }
                }
            }

            if (wallData.Production != null)
            {
                foreach (var point in wallData.Production)
                {
                    if (!BuildingService.Blocked(point.X, point.Y, 1, -.5f))
                    {
                        var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                        if (wallBlock != null)
                        {
                            commands.AddRange(wallBlock);
                            return true; ;
                        }
                    }
                }
            }

            return false;
        }

        float CalculateStartAngle(Vector2 baseLocation, Vector2 targetPosition)
        {
            var direction = targetPosition - baseLocation;
            return MathF.Atan2(direction.Y, direction.X);
        }

        Vector2 CalculateNextCirclePoint(Vector2 baseLocation, float radius, int pointCount, float startAngle, bool clockwise)
        {
            var angleIncrement = 2 * MathF.PI / pointCount;

            var angle = startAngle + angleIncrement;
            if (!clockwise)
            {
                angle = startAngle - angleIncrement;
            }

            var x = baseLocation.X + radius * MathF.Cos(angle);
            var y = baseLocation.Y + radius * MathF.Sin(angle);
            return new Vector2(x, y);
        }
    }
}
