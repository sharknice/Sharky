using Sharky.Extensions;

namespace Sharky.MicroTasks
{
    public class ProxyTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        MicroData MicroData;
        IIndividualMicroController IndividualMicroController;

        public int DesiredWorkers { get; set; }
        public List<DesiredUnitsClaim> DesiredDefendingUnitsClaims { get; set; }

        public string ProxyName { get; set; }
        bool started { get; set; }

        public ProxyTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, string proxyName, IIndividualMicroController individualMicroController, int desiredWorkers = 1)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            Priority = priority;
            MacroData = defaultSharkyBot.MacroData;
            ProxyName = proxyName;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            IndividualMicroController = individualMicroController;
            MicroData = defaultSharkyBot.MicroData;

            DesiredDefendingUnitsClaims = new List<DesiredUnitsClaim>();
            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            DesiredWorkers = desiredWorkers;
        }

        public override void Enable()
        {
            Enabled = true;
            started = false;
            if (MacroData.Proxies.ContainsKey(ProxyName))
            {
                MacroData.Proxies[ProxyName].Enabled = true;
            }
        }

        public override void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
                commander.UnitRole = UnitRole.None;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
            if (MacroData.Proxies.ContainsKey(ProxyName))
            {
                MacroData.Proxies[ProxyName].Enabled = false;
            }
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredWorkers)
            {
                if (started && DesiredWorkers == 1)
                {
                    Disable();
                    return;
                }

                var commander = ActiveUnitData.Commanders.Values.Where(c => c.UnitRole == UnitRole.Build && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && c.UnitCalculation.Unit.Orders.Any(o => ActiveUnitData.SelfUnits.Values.Any(s => s.Attributes.Contains(SC2Attribute.Structure) && s.Unit.BuildProgress == 1 && o.TargetWorldSpacePos != null && s.Position.X == o.TargetWorldSpacePos.X && s.Position.Y == o.TargetWorldSpacePos.Y))).Concat(
                    ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.PreBuild || c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
                    .OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, new Vector2(MacroData.Proxies[ProxyName].Location.X, MacroData.Proxies[ProxyName].Location.Y))).FirstOrDefault();

                if (commander != null)
                {
                    commander.UnitRole = UnitRole.Proxy;
                    commander.Claimed = true;
                    UnitCommanders.Add(commander);
                    started = true;
                    return;
                }
            }

            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredDefendingUnitsClaims)
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
                count += UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
            }
            return count < desiredUnitClaim.Count;
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var commands = new List<SC2Action>();

            if (MacroData.Proxies.ContainsKey(ProxyName))
            {
                commands.AddRange(MoveToProxyLocation(frame));
            }

            return commands;
        }

        IEnumerable<SC2Action> MoveToProxyLocation(int frame)
        {
            var commands = new List<SC2Action>();

            foreach (var commander in UnitCommanders.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
            {
                if (commander.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker))
                {
                    OrderWorker(frame, commands, commander);
                }
                else
                {
                    OrderDefender(frame, commands, commander);
                }
            }

            return commands;
        }

        private void OrderWorker(int frame, List<SC2Action> commands, UnitCommander commander)
        {
            if (commander.UnitRole != UnitRole.Proxy && commander.UnitRole != UnitRole.Build)
            {
                commander.UnitRole = UnitRole.Proxy;
            }

            if (!MacroData.Proxies[ProxyName].DefendProxyLocation && Vector2.Distance(MacroData.Proxies[ProxyName].Location.ToVector2(), commander.UnitCalculation.Position) < 5)
            {
                if (MacroData.Proxies[ProxyName].DesiredPylons > 0)
                {
                    var pylons = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
                    foreach(var pylon in pylons)
                    {
                        ActiveUnitData.Commanders[pylon.Unit.Tag].UnitRole = UnitRole.DoNotDefend;
                    }
                }
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
            {
                var action = IndividualMicroController.Retreat(commander, MacroData.Proxies[ProxyName].Location, null, frame);
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }
            else if (Vector2.DistanceSquared(MacroData.Proxies[ProxyName].Location.ToVector2(), commander.UnitCalculation.Position) > MacroData.Proxies[ProxyName].MaximumBuildingDistance)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround))
                {
                    List<SC2Action> action;
                    if (IndividualMicroController.NavigateToTarget(commander, MacroData.Proxies[ProxyName].Location, null, null, Formation.Normal, frame, out action))
                    {
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
                else
                {
                    var action = commander.Order(frame, Abilities.MOVE, MacroData.Proxies[ProxyName].Location);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }
        }

        private void OrderDefender(int frame, List<SC2Action> commands, UnitCommander commander)
        {
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, MacroData.Proxies[ProxyName].Location.ToVector2()) < 225)
            {
                List<SC2Action> action;
                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Attack(commander, MacroData.Proxies[ProxyName].Location, MacroData.Proxies[ProxyName].Location, null, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Attack(commander, MacroData.Proxies[ProxyName].Location, MacroData.Proxies[ProxyName].Location, null, frame);
                }
                if (action != null) { commands.AddRange(action); }
            }
            else
            {
                List<SC2Action> action;
                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.NavigateToPoint(commander, MacroData.Proxies[ProxyName].Location, MacroData.Proxies[ProxyName].Location, null, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.NavigateToPoint(commander, MacroData.Proxies[ProxyName].Location, MacroData.Proxies[ProxyName].Location, null, frame);
                }
                if (action != null) { commands.AddRange(action); }
            }
        }
    }
}
