using Sharky.DefaultBot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class RepairTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        Dictionary<ulong, RepairData> RepairData;

        public RepairTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            RepairData = new Dictionary<ulong, RepairData>();
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (MacroData.Minerals > 10 && EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                foreach (var repair in RepairData)
                {
                    var needed = repair.Value.DesiredRepairers - repair.Value.Repairers.Count();
                    if (needed > 0)
                    {
                        var closest = commanders.Where(commander => !commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV)
                            .OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, repair.Value.UnitToRepair.Position)).Take(needed);
                        foreach(var scv in closest)
                        {
                            scv.Value.Claimed = true;
                            scv.Value.UnitRole = UnitRole.Repair;
                            UnitCommanders.Add(scv.Value);
                            repair.Value.Repairers.Add(scv.Value);
                        }
                    }
                    needed = repair.Value.DesiredRepairers - repair.Value.Repairers.Count();
                    if (needed > 0)
                    {
                        var closest = commanders.Where(commander => commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && 
                            (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.None) && commander.Value.UnitCalculation.Unit.BuffIds.Count() == 0)
                                .OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, repair.Value.UnitToRepair.Position)).Take(needed);
                        foreach (var scv in closest)
                        {
                            scv.Value.Claimed = true;
                            scv.Value.UnitRole = UnitRole.Repair;
                            UnitCommanders.Add(scv.Value);
                            repair.Value.Repairers.Add(scv.Value);
                        }
                    }
                }
            }
        }


        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                UpdateRepairData(frame);

                foreach (var repair in RepairData)
                {
                    foreach (var scv in repair.Value.Repairers)
                    {
                        var action = scv.Order(frame, Abilities.EFFECT_REPAIR, targetTag: repair.Key);
                        if (action != null) { actions.AddRange(action); }
                    }
                }
            }

            return actions;
        }

        private void UpdateRepairData(int frame)
        {
            foreach (var building in ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.BuildProgress == 1 && u.Unit.Health < u.Unit.HealthMax && u.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
            {
                if (!RepairData.ContainsKey(building.Unit.Tag))
                {
                    RepairData[building.Unit.Tag] = new RepairData(building);
                }
                else
                {
                    RepairData[building.Unit.Tag].UnitToRepair = building;
                    RepairData[building.Unit.Tag].Repairers.RemoveAll(s => s.UnitCalculation.FrameLastSeen != frame);
                }
            }

            RemoveFinishedData(frame);

            CalculateRepair(frame);
        }


        private void RemoveFinishedData(int frame)
        {
            var doneList = RepairData.Where(d => d.Value.UnitToRepair.FrameLastSeen != frame).Select(d => d.Key);
            foreach (var key in doneList)
            {
                foreach (var scv in RepairData[key].Repairers)
                {
                    scv.Claimed = false;
                    scv.UnitRole = UnitRole.None;
                    UnitCommanders.RemoveAll(u => u.UnitCalculation.Unit.Tag == scv.UnitCalculation.Unit.Tag);
                }
                RepairData.Remove(key);
            }
        }

        private void CalculateRepair(int frame)
        {
            foreach (var repair in RepairData)
            {
                var dps = repair.Value.UnitToRepair.Attackers.Sum(a => a.Dps);
                var singleHps = (repair.Value.UnitToRepair.Unit.HealthMax / SharkyUnitData.UnitData[(UnitTypes)repair.Value.UnitToRepair.Unit.UnitType].BuildTime) * SharkyOptions.FramesPerSecond;
                repair.Value.DesiredRepairers = (int)Math.Ceiling(dps / singleHps);
                if (repair.Value.DesiredRepairers == 0)
                {
                    repair.Value.DesiredRepairers = 1;
                }
            }
        }
    }
}
