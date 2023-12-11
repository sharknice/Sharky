using Sharky.DefaultBot;

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

        bool started { get; set; }

        public bool BlockAddons { get; set; }

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

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            LateGame = false;
            BlockAddons = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
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

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitRole != UnitRole.Scout) { commander.UnitRole = UnitRole.Scout; }

                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && (e.UnitClassifications.Contains(UnitClassification.Worker) || e.Attributes.Contains(SC2Attribute.Structure))) && commander.UnitCalculation.NearbyEnemies.Count() < 5)
                {
                    if (BlockAddons && (MacroData.Minerals > 100 || commander.LastAbility == Abilities.BUILD_PYLON) && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
                    {
                        var buildingWithoutAddon = commander.UnitCalculation.NearbyEnemies.Where(e => (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || e.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || e.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT) && !e.Unit.HasAddOnTag && BuildingBuilder.HasRoomForAddon(e.Unit)).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (buildingWithoutAddon != null)
                        {
                            if (buildingWithoutAddon.Unit.BuildProgress >= .75f || commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)))
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

                    if (commander.UnitCalculation.Unit.Shield < 5)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageGround) > 0)
                        {
                            var threat = commander.UnitCalculation.EnemiesThreateningDamage.FirstOrDefault();
                            if (threat != null)
                            {
                                if (threat.SimulatedHitpoints > commander.UnitCalculation.SimulatedHitpoints)
                                {
                                    var bait = IndividualMicroController.Retreat(commander, TargetingData.ForwardDefensePoint, null, frame);
                                    if (bait != null)
                                    {
                                        commands.AddRange(bait);
                                    }
                                    continue;
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
            foreach (var baseLocation in BaseData.BaseLocations.Skip(1).Take(4))
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
