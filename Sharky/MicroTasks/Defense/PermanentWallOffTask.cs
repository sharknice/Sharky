namespace Sharky.MicroTasks
{
    public class PermanentWallOffTask: WallOffTask
    {
        public bool UsePylon { get; set; }
        public bool ToggleMiningDefense { get; set; } = false;
        bool ShieldBatteryExists;
        protected MicroTaskData MicroTaskData;
        RequirementService RequirementService;

        public PermanentWallOffTask(SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, MicroTaskData microTaskData, MacroData macroData, MapData mapData, WallService wallService, ChatService chatService, RequirementService requirementService, bool enabled, float priority)
            : base(sharkyUnitData, activeUnitData, macroData, mapData, wallService, chatService, enabled, priority)
        {
            ShieldBatteryExists = false;
            UsePylon = false;
            MicroTaskData = microTaskData;
            RequirementService = requirementService;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (!UnitCommanders.Any() && ProbeSpot != null && !ShieldBatteryExists)
            {
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(c => DistanceToResourceCenter(c)).ThenBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, ProbeSpot.ToVector2())).Where(c => Vector2.Distance(c.Value.UnitCalculation.Position, ProbeSpot.ToVector2()) < 50))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        MicroTaskData.StealCommanderFromAllTasks(commander.Value);
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        return;                   
                    }
                }
            }
        }

        public override void Disable()
        {
            if (ToggleMiningDefense)
            {
                ((MiningTask)MicroTaskData[typeof(MiningTask).Name]).MiningDefenseService.Enabled = true;
            }
            base.Disable();
        }
         
        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            GetWallData();

            var commands = new List<SC2APIProtocol.Action>();
            if (WallData == null || WallData.Block == null || WallData.Pylons == null)
            {
                return commands;
            }

            var pylonPosition = WallData.Pylons.FirstOrDefault();
            if (pylonPosition != null)
            {
                var probe = UnitCommanders.FirstOrDefault();
                if (probe == null) { return commands; }
                if (probe.UnitRole != UnitRole.Wall) 
                { 
                    probe.UnitRole = UnitRole.Wall; 
                }
                if (probe.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.HARVEST_GATHER_PROBE))
                {
                    var probeCommand = probe.Order(frame, Abilities.STOP, allowSpam: true);
                    if (probeCommand != null)
                    {
                        commands.AddRange(probeCommand);
                    }

                    return commands;
                }

                var blockBuilding = ActiveUnitData.Commanders.FirstOrDefault(u => (u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY || (UsePylon && u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON)) && u.Value.UnitCalculation.Unit.Pos.X == WallData.Block.X && u.Value.UnitCalculation.Unit.Pos.Y == WallData.Block.Y).Value;

                if (blockBuilding != null)
                {
                    ShieldBatteryExists = true;
                    if (!BlockedChatSent)
                    {
                        ChatService.SendChatType("PermanentWallOffTask-TaskCompleted");
                        BlockedChatSent = true;
                        probe.UnitRole = UnitRole.None;
                        probe.Claimed = false;
                        UnitCommanders.Remove(probe);
                    }
                }
                else if (probe != null)
                {
                    var pylon = ActiveUnitData.SelfUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.Unit.Pos.X == pylonPosition.X && u.Value.Unit.Pos.Y == pylonPosition.Y).Value;
                    if (pylon != null)
                    {
                        if (((probe.LastAbility == Abilities.BUILD_SHIELDBATTERY || probe.LastAbility == Abilities.BUILD_PYLON) && frame - probe.LastOrderFrame < 4) || probe.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON || o.AbilityId == (uint)Abilities.BUILD_SHIELDBATTERY))
                        {
                            return commands;
                        }
                        if (MacroData.Minerals >= 100)
                        {
                            var ability = Abilities.BUILD_SHIELDBATTERY;
                            if (UsePylon == true || !RequirementService.HaveCompleted(UnitTypes.PROTOSS_CYBERNETICSCORE))
                            {
                                UsePylon = true;
                                ability = Abilities.BUILD_PYLON;
                            }
                            var probeCommand = probe.Order(frame, ability, WallData.Block, allowSpam: true);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                        else
                        {
                            if (Vector2.DistanceSquared(probe.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > 4)
                            {
                                var probeCommand = probe.Order(frame, Abilities.MOVE, ProbeSpot);
                                if (probeCommand != null)
                                {
                                    commands.AddRange(probeCommand);
                                }

                                return commands;
                            }
                        }
                    }
                }
                else
                {
                    ShieldBatteryExists = false;
                }
            }

            if (ToggleMiningDefense)
            {
                ((MiningTask)MicroTaskData[typeof(MiningTask).Name]).MiningDefenseService.Enabled = !ShieldBatteryExists;
            }

            return commands;
        }
    }
}
