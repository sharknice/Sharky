namespace Sharky.MicroTasks
{
    public class ProxyScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        BaseData BaseData;
        SharkyOptions SharkyOptions;
        MacroData MacroData;
        BuildingService BuildingService;
        IBuildingBuilder BuildingBuilder;
        IIndividualMicroController IndividualMicroController;
        MapDataService MapDataService;
        MicroTaskData MicroTaskData;
        ActiveUnitData ActiveUnitData;
        RequirementService RequirementService;

        public bool Started { get; set; }

        public bool BlockAddons { get; set; }
        public bool PylonPylons { get; set; }
        public bool BuildCannon { get; set; }

        List<Point2D> ScoutLocations { get; set; }
        int ScoutLocationIndex { get; set; }
        bool LateGame;

        public ProxyScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            BuildingBuilder = defaultSharkyBot.BuildingBuilder;
            BuildingService = defaultSharkyBot.BuildingService;
            MacroData = defaultSharkyBot.MacroData;
            Priority = priority;
            IndividualMicroController = individualMicroController;
            MapDataService = defaultSharkyBot.MapDataService;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            RequirementService = defaultSharkyBot.RequirementService;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            LateGame = false;
            BlockAddons = false;
            PylonPylons = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (Started)
                {
                    Disable();
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
                                Started = true;
                                return;
                            }
                        }
                    }
                    if (scouter.Value == null)
                    {
                        scouter = available.OrderBy(c => Vector2.DistanceSquared(BaseData.BaseLocations.Skip(1).FirstOrDefault().Location.ToVector2(), c.Value.UnitCalculation.Position)).FirstOrDefault();
                        foreach (var task in MicroTaskData)
                        {
                            if (task.Value.UnitCommanders != null && task.Value.UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == scouter.Value.UnitCalculation.Unit.Tag))
                            {
                                task.Value.StealUnit(scouter.Value);
                            }
                        }
                    }
                }

                if (scouter.Value != null)
                {
                    scouter.Value.Claimed = true;
                    scouter.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(scouter.Value);
                    Started = true;
                    return;
                }
            }
        }

        public void ManualClaim(UnitCommander commander)
        {
            commander.Claimed = true;
            commander.UnitRole = UnitRole.Scout;
            UnitCommanders.Add(commander);
            Started = true;
            return;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (ScoutLocations == null)
            {
                GetScoutLocations();
            }
            if (!LateGame && frame > SharkyOptions.FramesPerSecond * 4 * 60)
            {
                LateGame = true;
                ScoutLocations = new List<Point2D>();
                foreach (var baseLocation in BaseData.BaseLocations.Where(b => !BaseData.SelfBases.Any(s => s.Location == b.Location) && !BaseData.EnemyBases.Any(s => s.Location == b.Location)))
                {
                    ScoutLocations.AddRange(GetPointsForLocation(baseLocation));
                }
                ScoutLocationIndex = 0;
            }

            var commands = new List<SC2APIProtocol.Action>();

            var enemyProxies = ActiveUnitData.EnemyUnits.Values.Where(e => e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(e.Position, TargetingData.SelfMainBasePoint.ToVector2()) < Vector2.DistanceSquared(e.Position, TargetingData.EnemyMainBasePoint.ToVector2()));

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitRole != UnitRole.Scout) { commander.UnitRole = UnitRole.Scout; }

                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && (e.UnitClassifications.HasFlag(UnitClassification.Worker) || e.Attributes.Contains(SC2Attribute.Structure))) && commander.UnitCalculation.NearbyEnemies.Count() < 5)
                {
                    if ((BlockAddons || PylonPylons) && (MacroData.Minerals >= 100 || commander.LastAbility == Abilities.BUILD_PYLON) && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                    {
                        if (BlockAddons)
                        {
                            var buildingsWithoutAddon = commander.UnitCalculation.NearbyEnemies.Where(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || e.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || e.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT) && !e.Unit.HasAddOnTag && BuildingBuilder.HasRoomForAddon(e.Unit)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position));
                            if (buildingsWithoutAddon.Count() == 1)
                            {
                                var buildingWithoutAddon = buildingsWithoutAddon.FirstOrDefault();
                                if (buildingWithoutAddon != null)
                                {
                                    if (buildingWithoutAddon.Unit.BuildProgress >= .75f || commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
                                    {
                                        var point = new Point2D { X = buildingWithoutAddon.Unit.Pos.X + 2.5f, Y = buildingWithoutAddon.Unit.Pos.Y - .5f };
                                        if (!BuildingService.BlocksResourceCenter(point.X, point.Y, 1))
                                        {
                                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                                            if (wallBlock != null)
                                            {
                                                commands.AddRange(wallBlock);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (PylonPylons)
                        {
                            var pylonsWithoutBuilding = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && !e.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && !e.NearbyEnemies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.Distance(e.Position, a.Position) < 6)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position));
                            if (pylonsWithoutBuilding.Count() == 1)
                            {
                                var pylonWithoutBuilding = pylonsWithoutBuilding.FirstOrDefault();
                                if (pylonWithoutBuilding != null)
                                {
                                    if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
                                    {
                                        var point = new Point2D { X = pylonWithoutBuilding.Unit.Pos.X + 2f, Y = pylonWithoutBuilding.Unit.Pos.Y };
                                        if (!BuildingService.BlocksResourceCenter(point.X, point.Y, 1))
                                        {
                                            var wallBlock = commander.Order(frame, Abilities.BUILD_PYLON, point);
                                            if (wallBlock != null)
                                            {
                                                commands.AddRange(wallBlock);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (BuildCannon)
                    {
                        if (RequirementService.HaveCompleted(UnitTypes.PROTOSS_FORGE))
                        {
                            if (commander.LastAbility == Abilities.BUILD_PHOTONCANNON && commander.LastOrderFrame + 100 > frame)
                            {
                                continue;
                            }

                            if (MacroData.Minerals >= 150)
                            {
                                var pylon = commander.UnitCalculation.NearbyAllies.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && e.Unit.BuildProgress == 1 && e.NearbyEnemies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.Distance(a.Position, e.Position) < 6));
                                if (pylon != null)
                                {
                                    if (pylon.NearbyEnemies.Count(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && Vector2.Distance(a.Position, pylon.Position) < 8) < 2)
                                    {
                                        var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PHOTONCANNON, SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PHOTONCANNON], pylon.Position.ToPoint2D(), true, 8, UnitCommanders);
                                        if (command != null)
                                        {
                                            commander.LastOrderFrame = frame;
                                            commander.LastAbility = Abilities.BUILD_PHOTONCANNON;
                                            commands.AddRange(command);
                                            continue;
                                        }
                                    }
                                }
                            }

                        }
                    }

                    if (commander.UnitCalculation.Unit.Shield < 5)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageGround) == 1)
                        {
                            var threat = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.DamageGround);
                            if (threat.Unit.Health + threat.Unit.Shield > commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Health)
                            {
                                var bait = IndividualMicroController.Retreat(commander, TargetingData.ForwardDefensePoint, null, frame);
                                if (bait != null)
                                {
                                    commands.AddRange(bait);
                                }
                                continue;
                            }
                        }
                        else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround))
                        {
                            var bait = IndividualMicroController.Retreat(commander, TargetingData.ForwardDefensePoint, null, frame);
                            if (bait != null)
                            {
                                commands.AddRange(bait);
                            }
                            continue;
                        }                     
                    }

                    var probes = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && e.FrameLastSeen == frame);
                    if (probes.Count() > 1)
                    {
                        var pylon = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON).OrderBy(e => Vector2.Distance(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (pylon != null)
                        {
                            var closestProbe = probes.OrderBy(e => Vector2.Distance(e.Position, pylon.Position)).FirstOrDefault();
                            commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: closestProbe.Unit.Tag));
                            continue;
                        }
                    }

                    if (enemyProxies.Any())
                    {
                        var closest = enemyProxies.OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (closest != null)
                        {
                            if (closest.FrameLastSeen != frame || Vector2.Distance(commander.UnitCalculation.Position, closest.Position) > 15)
                            {
                                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
                                {
                                    var paction = IndividualMicroController.Scout(commander, closest.Position.ToPoint2D(), TargetingData.ForwardDefensePoint, frame);
                                    if (paction != null)
                                    {
                                        commands.AddRange(paction);
                                    }
                                }
                                else
                                {
                                    var paction = commander.Order(frame, Abilities.MOVE, closest.Position.ToPoint2D());
                                    if (paction != null)
                                    {
                                        commands.AddRange(paction);
                                    }
                                }
                            }
                        }
                    }

                    var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault();
                    var action = IndividualMicroController.Attack(commander, new Point2D { X = enemy.Unit.Pos.X, Y = enemy.Unit.Pos.Y }, TargetingData.ForwardDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                else if (enemyProxies.Any())
                {
                    var closest = enemyProxies.OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (closest != null)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
                        {
                            var action = IndividualMicroController.Scout(commander, closest.Position.ToPoint2D(), TargetingData.ForwardDefensePoint, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                        else
                        {
                            var action = commander.Order(frame, Abilities.MOVE, closest.Position.ToPoint2D());
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                }
                else if (Vector2.DistanceSquared(new Vector2(ScoutLocations[ScoutLocationIndex].X, ScoutLocations[ScoutLocationIndex].Y), commander.UnitCalculation.Position) < 4)
                {
                    ScoutLocationIndex++;
                    if (ScoutLocationIndex >= ScoutLocations.Count())
                    {
                        ScoutLocationIndex = 0;
                    }
                }
                else
                {
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
                    {
                        var action = IndividualMicroController.Scout(commander, ScoutLocations[ScoutLocationIndex], TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var action = commander.Order(frame, Abilities.MOVE, ScoutLocations[ScoutLocationIndex]);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
            }

            return commands;
        }

        private void GetScoutLocations()
        {
            ScoutLocations = new List<Point2D>();
            foreach (var baseLocation in BaseData.BaseLocations.Skip(1).Take(5))
            {
                ScoutLocations.AddRange(GetPointsForLocation(baseLocation));
            }
            ScoutLocationIndex = 0;
        }

        List<Point2D> GetPointsForLocation(BaseLocation baseLocation)
        {
            var points = new List<Point2D>
            {
                new Point2D { X = baseLocation.Location.X - 5, Y = baseLocation.Location.Y - 5 },
                new Point2D { X = baseLocation.Location.X - 5, Y = baseLocation.Location.Y + 5 },
                new Point2D { X = baseLocation.Location.X + 5, Y = baseLocation.Location.Y + 5 },
                new Point2D { X = baseLocation.Location.X + 5, Y = baseLocation.Location.Y - 5 }
            };
            points.RemoveAll(p => !MapDataService.PathWalkable(p));
            return points;
        }
    }
}
