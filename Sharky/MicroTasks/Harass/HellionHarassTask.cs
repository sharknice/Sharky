namespace Sharky.MicroTasks
{
    public class HellionHarassTask : MicroTask
    {
        protected BaseData BaseData;
        protected TargetingData TargetingData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        MicroTaskData MicroTaskData;
        SharkyOptions SharkyOptions;
        FrameToTimeConverter FrameToTimeConverter;
        MapDataService MapDataService;
        EnemyData EnemyData;

        public IIndividualMicroController HellionMicroController { get; set; }
        public IIndividualMicroController ReaperMicroController { get; set; }

        public bool Started { get; set; }

        public int DesiredHellions { get; set; }

        protected Point2D AttackPoint { get; set; }

        public Dictionary<ulong, UnitCalculation> Kills { get; private set; }

        UnitCommander Reaper { get; set; }

        bool WaitAndHide { get; set; }
        bool DoneHiding { get; set; }

        public HellionHarassTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController hellionMicroController, IIndividualMicroController reaperMicroController)
        {
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            MapDataService = defaultSharkyBot.MapDataService;
            EnemyData = defaultSharkyBot.EnemyData;

            HellionMicroController = hellionMicroController;
            ReaperMicroController = reaperMicroController;

            UnitCommanders = new List<UnitCommander>();

            DesiredHellions = 4;

            Enabled = enabled;
            Priority = priority;

            Kills = new Dictionary<ulong, UnitCalculation>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count(e => e.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION) < DesiredHellions)
            {
                if (Started)
                {
                    if (!UnitCommanders.Any(e => e.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION))
                    {
                        Console.WriteLine($"HellionHarass ended");
                        Disable();
                    }
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count(e => e.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION) >= DesiredHellions)
                        {
                            Started = true;
                            return;
                        }
                    }
                    if (Reaper == null)
                    {
                        if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER && !MicroTaskData[typeof(ReaperWorkerHarassTask).Name].Enabled)
                        {
                            MicroTaskData[typeof(AttackTask).Name].StealUnit(commander.Value);
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Harass;
                            UnitCommanders.Add(commander.Value);
                            Reaper = commander.Value;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (!Started && Reaper != null)
            {
                if (Vector2.DistanceSquared(Reaper.UnitCalculation.Position, TargetingData.ForwardDefensePoint.ToVector2()) > 400)
                {
                    var action = ReaperMicroController.Scout(Reaper, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, frame, attack: false);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var action = ReaperMicroController.Retreat(Reaper, TargetingData.ForwardDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            if (BaseData?.EnemyBaseLocations?.FirstOrDefault() == null || !Started) 
            {
                foreach (var commander in UnitCommanders)
                {
                    var action = HellionMicroController.Attack(commander, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }

                return commands;              
            }

            if (AttackPoint == null)
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().BehindMineralLineLocation;
            }

            if (Started && !DoneHiding)
            {
                if (WaitAndHide)
                {
                    return HideHellionsAndReaper(frame);
                }
                else
                {
                    var enemyNatural = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().Location;
                    if (MapDataService.SelfVisible(enemyNatural) && !BaseData.EnemyBases.Any(b => b.Location.X == enemyNatural.X && b.Location.Y == enemyNatural.Y && b.ResourceCenter != null && b.ResourceCenter.BuildProgress >= 1))
                    {
                        WaitAndHide = true;
                        AttackPoint = BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation;
                    }
                    else
                    {
                        WaitAndHide = false;
                    }
                }
            }

            commands.AddRange(OrderHellions(frame));

            if (Reaper != null)
            {
                var attackLocation = BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation;
                var problemForHellions = Reaper.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !e.Unit.IsFlying && (e.EnemiesInRangeOf.Count(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION) > 1 || e.EnemiesInRange.Count(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION) > 1));
                if (problemForHellions != null)
                {
                    if (Reaper.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_KD8CHARGE) || Reaper.AbilityOffCooldown(Abilities.EFFECT_KD8CHARGE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
                    {
                        var action = Reaper.Order(frame, Abilities.EFFECT_KD8CHARGE, problemForHellions.Unit.Pos.ToPoint2D(), allowSpam: true);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        return commands;
                    }
                }
                if (Vector2.DistanceSquared(Reaper.UnitCalculation.Position, attackLocation.ToVector2()) < 400 && MapDataService.MapHeight(Reaper.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(attackLocation))
                {
                    var action = ReaperMicroController.HarassWorkers(Reaper, attackLocation, TargetingData.ForwardDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    if (!Reaper.UnitCalculation.EnemiesThreateningDamage.Any())
                    {
                        var action = Reaper.Order(frame, Abilities.MOVE, attackLocation);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var action = ReaperMicroController.NavigateToPoint(Reaper, attackLocation, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
            }

            return commands;
        }

        protected virtual List<SC2APIProtocol.Action> OrderHellions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.TERRAN_HELLION) 
                {
                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLIONTANK)
                    {
                        var action = commander.Order(frame, Abilities.MORPH_HELLION);
                        if (action != null) { commands.AddRange(action); }
                    }
                    continue;
                }

                // kill any workers in range
                if (commander.UnitCalculation.Unit.WeaponCooldown < 2 && commander.UnitCalculation.EnemiesInRange.Any(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.Worker)))
                {
                    var action = HellionMicroController.HarassWorkers(commander, AttackPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                // kill any unguarded workers
                if (commander.UnitCalculation.Unit.WeaponCooldown < 2 && commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.Worker)) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    var action = HellionMicroController.HarassWorkers(commander, AttackPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                // kill clumps of workers
                if (commander.UnitCalculation.EnemiesInRange.Count(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.Worker)) > 2)
                {
                    var action = HellionMicroController.HarassWorkers(commander, AttackPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                // go straight to mineral line, ignoring enemies
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, AttackPoint.ToVector2()) > 100)
                {
                    commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                    List<SC2APIProtocol.Action> action = null;
                    action = commander.Order(frame, Abilities.MOVE, AttackPoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                // kill workers at mineral line
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                {
                    var action = HellionMicroController.HarassWorkers(commander, AttackPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                else
                {
                    SwitchTargetLocation();
                }
            }

            return commands;
        }

        protected virtual void SwitchTargetLocation()
        {
            // switch between main and natural
            if (AttackPoint != BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation)
            {
                AttackPoint = BaseData.EnemyBaseLocations.FirstOrDefault().BehindMineralLineLocation;
            }
            else
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().BehindMineralLineLocation;
            }
        }

        private IEnumerable<SC2APIProtocol.Action> HideHellionsAndReaper(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyData.EnemyAggressivityData.IsHarassing || EnemyData.EnemyAggressivityData.ArmyAggressivity > .5f)
            {
                DoneHiding = true;
            }

            var behindEnemyNatural = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().BehindMineralLineLocation;

            foreach (var commander in UnitCommanders)
            {
                if (!commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, behindEnemyNatural.ToVector2()) > 9)
                    {
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, behindEnemyNatural));
                    }
                }
                else
                {
                    var threat = commander.UnitCalculation.EnemiesThreateningDamage.FirstOrDefault();
                    var ramp = TargetingData.ChokePoints.Bad.FirstOrDefault();
                    if (ramp != null && threat != null)
                    {
                        var distanceToRamp = Vector2.DistanceSquared(commander.UnitCalculation.Position, ramp.Center);
                        if (distanceToRamp < 36 && distanceToRamp < Vector2.DistanceSquared(threat.Position, ramp.Center))
                        {
                            DoneHiding = true;
                        }

                    }
                    var retreatLocation = BaseData.EnemyBaseLocations.Skip(2).FirstOrDefault().Location;
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, retreatLocation.ToVector2()) < 100)
                    {
                        retreatLocation = TargetingData.ForwardDefensePoint;
                    }
                    var action = HellionMicroController.Retreat(commander, retreatLocation, null, frame);
                    if (action != null) { commands.AddRange(action); }
                }
            }

            return commands;
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
                if (Reaper != null && Reaper.UnitCalculation.Unit.Tag == tag)
                {
                    Reaper = null;
                }
            }

            if (deaths > 0)
            {
                // TODO: chat for deaths, it was worth it, that didn't go well, etc.
                Deaths += deaths;
            }
            if (kills > 0 || deaths > 0)
            {
                // TODO: chat for kills
                ReportResults();
            }
        }

        private void ReportResults()
        {
            Console.WriteLine($"{FrameToTimeConverter.GetTime(MacroData.Frame)} HellionHarass Report: Deaths:{Deaths}, Kills:{Kills.Count()}");
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
