﻿namespace Sharky.MicroTasks
{
    public class DefensiveZealotWarpInTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        SharkyOptions SharkyOptions;
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;

        WarpInPlacement WarpInPlacement;

        public DefensiveZealotWarpInTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MacroData = defaultSharkyBot.MacroData;
            WarpInPlacement = (WarpInPlacement)defaultSharkyBot.WarpInPlacement;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var commands = new List<SC2Action>();

            if (MacroData.Minerals < 100 || MacroData.FoodLeft < 2) { return commands; }

            var idleWarpGate = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE && c.WarpInOffCooldown(frame, SharkyOptions.FramesPerSecond, SharkyUnitData));
            if (idleWarpGate == null)
            {
                return commands;
            }

            foreach (var pylon in ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Unit.BuildProgress >= 1))
            {
                if (pylon.NearbyEnemies.Any(e => e.FrameLastSeen == frame && e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !e.Unit.IsFlying && !e.Unit.IsHallucination && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING && e.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELINGZEALOT))
                {
                    if (pylon.TargetPriorityCalculation.GroundWinnability < 1 || !pylon.NearbyAllies.Any(a => a.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
                    {
                        var location = WarpInPlacement.FindPlacementForPylon(pylon, 1);
                        if (location != null)
                        {
                            var action = idleWarpGate.Order(frame, Abilities.TRAINWARP_ZEALOT, location);
                            if (action != null)
                            {
                                commands.AddRange(action);
                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }
    }
}
