namespace Sharky.MicroTasks
{
    public class PermanentWallOffTask: WallOffTask
    {
        public bool UsePylon { get; set; }
        bool ShieldBatteryExists;

        public PermanentWallOffTask(SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, MacroData macroData, MapData mapData, WallService wallService, ChatService chatService, bool enabled, float priority)
            : base(sharkyUnitData, activeUnitData, macroData, mapData, wallService, chatService, enabled, priority)
        {
            ShieldBatteryExists = false;
            UsePylon = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (!UnitCommanders.Any() && ProbeSpot != null && !ShieldBatteryExists)
            {
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(c => DistanceToResourceCenter(c)).ThenBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y))))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {                    
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        return;                   
                    }
                }
            }
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
                if (probe.UnitRole != UnitRole.Wall) { probe.UnitRole = UnitRole.Wall; }

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
                        if (MacroData.Minerals >= 100)
                        {
                            var ability = Abilities.BUILD_SHIELDBATTERY;
                            if (UsePylon == true)
                            {
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

            return commands;
        }
    }
}
