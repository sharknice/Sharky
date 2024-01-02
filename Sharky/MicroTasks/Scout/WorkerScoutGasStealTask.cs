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
        public bool ScoutEntireAreaBeforeAttacking { get; set; }
        public bool OnlyBlockOnce { get; set; }
        bool BlockedOnce = false;

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
        CameraManager CameraManager;
        MicroTaskData MicroTaskData;

        protected MineralWalker MineralWalker;

        protected List<Point2D> ScoutPoints;
        protected List<Point2D> EnemyMainArea;
        protected List<Point2D> DangerArea;

        protected IIndividualMicroController IndividualMicroController;

        protected bool started { get; set; }

        UnitCalculation Pylon { get; set; }


        public WorkerScoutGasStealTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController individualMicroController)
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

                var workers = commanders.Where(commander => commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker));
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
                if (OnlyBlockOnce && BlockedOnce)
                {
                    var pylon = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
                    if (pylon != null)
                    {
                        Pylon = pylon;
                    }
                    if (Pylon != null && Pylon.Unit.Shield < 25 && Pylon.Unit.BuildProgress > .95f)
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

                if (commander.UnitCalculation.Unit.ShieldMax > 5 && (commander.UnitCalculation.Unit.Shield < 5 || (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.Contains(UnitClassification.Worker)) > 0)))
                {
                    if (MineralWalker.MineralWalkHome(commander, frame, out List<SC2APIProtocol.Action> mineralWalk))
                    {
                        commands.AddRange(mineralWalk);
                        continue;
                    }
                }

                if (TryRecallProbe(commander, frame, commands, disable))
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
                                if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), commander.UnitCalculation.Position) < 400 && commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker)) > 5)
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

                    var expansion = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                    if (expansion != null)
                    {
                        if (BlockExpansion || (BlockLiftedExpansion && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) > 1))
                        {
                            var expansionVector = new Vector2(expansion.Location.X, expansion.Location.Y);
                            if (Vector2.DistanceSquared(expansionVector, commander.UnitCalculation.Position) < 225)
                            {
                                if (!commander.UnitCalculation.NearbyAllies.Any(a => Vector2.DistanceSquared(expansionVector, a.Position) < 9) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ResourceCenter) && !e.Unit.IsFlying && Vector2.DistanceSquared(expansionVector, e.Position) < 2))
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
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: enemy.Unit.Tag));
                            continue;
                        }
                    }
                }

                if (frame % 50 == 0)
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                }

                var points = ScoutPoints.Where(p => Vector2.DistanceSquared(p.ToVector2(), commander.UnitCalculation.Position) < 36 || Vector2.DistanceSquared(p.ToVector2(), mainVector) < 36).OrderBy(p => MapDataService.LastFrameAlliesTouched(p)).ThenBy(p => Vector2.DistanceSquared(p.ToVector2(), mainVector)).ThenBy(p => Vector2.DistanceSquared(commander.UnitCalculation.Position, p.ToVector2()));
                var navpoint = points.FirstOrDefault(p => !MapDataService.PathBlocked(p.ToVector2()));
                if (EnemyData.EnemyRace == Race.Zerg)
                {
                    var baseLocation = BaseData.EnemyBaseLocations.FirstOrDefault();
                    if (baseLocation != null)
                    {
                        if (MapDataService.LastFrameAlliesTouched(baseLocation.BehindMineralLineLocation) < 1)
                        {
                            navpoint = baseLocation.BehindMineralLineLocation;
                        }
                    }
                }
                if (navpoint == null)
                {
                    navpoint = ScoutPoints.OrderBy(p => MapDataService.LastFrameAlliesTouched(p)).ThenBy(p => Vector2.DistanceSquared(p.ToVector2(), mainVector)).ThenBy(p => Vector2.DistanceSquared(commander.UnitCalculation.Position, p.ToVector2())).FirstOrDefault();
                }
                if (navpoint != null)
                {
                    if (commander.UnitCalculation.Unit.Shield > 15 && commander.UnitCalculation.NearbyEnemies.Any() && (commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) || (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ResourceCenter) && e.Unit.BuildProgress == 1) && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))))
                    {
                        if (!ScoutEntireAreaBeforeAttacking || ScoutPoints.All(p => MapDataService.LastFrameVisibility(p) > 100))
                        {
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, navpoint));
                            continue;
                        }
                    }

                    var action = commander.Order(frame, Abilities.MOVE, navpoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
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
                if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) == 1 && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ProductionStructure)))
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
    }
}
