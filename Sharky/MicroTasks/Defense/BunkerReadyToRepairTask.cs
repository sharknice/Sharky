using Sharky.DefaultBot;
using Sharky.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class BunkerReadyToRepairTask : MicroTask
    {
        TargetingData TargetingData;
        SharkyUnitData SharkyUnitData;
        MicroTaskData MicroTaskData;
        ActiveUnitData ActiveUnitData;

        public int DesiredScvs { get; set; }

        public BunkerReadyToRepairTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;


            UnitCommanders = new List<UnitCommander>();

            DesiredScvs = 5;

            Enabled = enabled;
            Priority = priority;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            var needed = DesiredScvs - UnitCommanders.Count(e => e.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
            if (needed > 0)
            {
                var vector = TargetingData.ForwardDefensePoint.ToVector2();
                var scvs = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && c.UnitCalculation.Unit.Orders.Any(o => ActiveUnitData.SelfUnits.Values.Any(s => s.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && s.Unit.BuildProgress == 1 && o.TargetWorldSpacePos != null && s.Position.X == o.TargetWorldSpacePos.X && s.Position.Y == o.TargetWorldSpacePos.Y))).Concat(
                    ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.PreBuild || c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
                    .Where(c => !UnitCommanders.Any(u => u.UnitCalculation.Unit.Tag == c.UnitCalculation.Unit.Tag))
                    .OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, vector)).Take(needed);
                foreach (var commander in scvs)
                {
                    MicroTaskData[typeof(MiningTask).Name].StealUnit(commander);
                    commander.Claimed = true;
                    commander.UnitRole = UnitRole.Repair;
                    UnitCommanders.Add(commander);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            var vector = TargetingData.ForwardDefensePoint.ToVector2();
            var bunkers = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER).OrderBy(c => c.UnitCalculation.Unit.BuildProgress).ThenBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector));

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.TERRAN_SCV) { continue; }

                if (!commander.AutoCastOff)
                {
                    var action = commander.ToggleAutoCast(Abilities.EFFECT_REPAIR_SCV);
                    commander.AutoCastOff = true;
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                if (commander.UnitCalculation.Loaded)
                {
                    var bunkerInside = ActiveUnitData.Commanders.Values.FirstOrDefault(a => a.UnitCalculation.Unit.Passengers.Any(p => p.Tag == commander.UnitCalculation.Unit.Tag));

                    if (bunkerInside != null)
                    {
                        var action = bunkerInside.UnloadSpecificUnit(frame, Abilities.UNLOADALLAT, commander.UnitCalculation.Unit.Tag);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    continue;
                }

                var bunker = bunkers.FirstOrDefault(b => b.UnitCalculation.Unit.Health < b.UnitCalculation.Unit.HealthMax);
                if (bunker != null) 
                {
                    var action = commander.Order(frame, Abilities.EFFECT_REPAIR, targetTag: bunker.UnitCalculation.Unit.Tag);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
                bunker = bunkers.FirstOrDefault();
                if (bunker != null)
                {
                    if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                    {
                        var action = commander.Order(frame, Abilities.SMART, targetTag: bunker.UnitCalculation.Unit.Tag);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                    else
                    {
                        var action = commander.Order(frame, Abilities.MOVE, targetTag: bunker.UnitCalculation.Unit.Tag);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, vector) > 25)
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetingData.ForwardDefensePoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }
            }

            return commands;
        }
    }
}
