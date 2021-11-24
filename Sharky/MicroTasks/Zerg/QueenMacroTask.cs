using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class QueenMacroTask : MicroTask
    {
        EnemyData EnemyData;
        ActiveUnitData ActiveUnitData;

        CreepTumorPlacementFinder CreepTumorPlacementFinder;
        ChatService ChatService;

        List<InjectData> InjectData;
        List<UnitCommander> CreepSpreaders;

        public int DesiredCreepSpreaders { get; set; }

        bool SpreadCreepActive;

        // TODO: have a list of the hatchery equivalants, and assign a queen to each one, use extra queens to spread creep
        // TODO: creep tumor spread task that spreads creep

        public QueenMacroTask(DefaultSharkyBot defaultSharkyBot, int desiredCreepSpreaders, float priority, bool enabled = true)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            ChatService = defaultSharkyBot.ChatService;

            DesiredCreepSpreaders = desiredCreepSpreaders;

            UnitCommanders = new List<UnitCommander>();
            InjectData = new List<InjectData>();
            CreepSpreaders = new List<UnitCommander>();

            SpreadCreepActive = true;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var data in InjectData)
            {
                if (data.Queen == null)
                {
                    var closest = commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !commander.Value.Claimed)
                            .OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, data.Hatchery.Position)).FirstOrDefault();
                    if (closest.Value != null)
                    {
                        closest.Value.Claimed = true;
                        closest.Value.UnitRole = UnitRole.SpawnLarva;
                        UnitCommanders.Add(closest.Value);
                        data.Queen = closest.Value;
                    }
                }
            }
            if (CreepSpreaders.Count() < DesiredCreepSpreaders)
            {
                var queen = commanders.FirstOrDefault(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !commander.Value.Claimed);
                if (queen.Value != null)
                {
                    queen.Value.Claimed = true;
                    queen.Value.UnitRole = UnitRole.SpreadCreep;
                    UnitCommanders.Add(queen.Value);
                    CreepSpreaders.Add(queen.Value);
                }
            }
        }


        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return actions;
            }

            UpdatData(frame);

            actions.AddRange(InjectLarva(frame));
            actions.AddRange(SpreadCreep(frame));

            return actions;
        }

        IEnumerable<SC2APIProtocol.Action> SpreadCreep(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (SpreadCreepActive)
            {
                foreach (var queen in CreepSpreaders)
                {
                    if (queen.UnitCalculation.Unit.Energy >= 25)
                    {
                        if (queen.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_CREEPTUMOR_QUEEN))
                        {
                            continue; // TODO: don't suicide and stuff
                        }
                        var spot = CreepTumorPlacementFinder.FindTumorPlacement();
                        if (spot == null)
                        {
                            SpreadCreepActive = false;
                            ChatService.SendChatType("SpreadCreepTask-TaskCompleted");
                            return actions;
                        }
                        var action = queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, spot);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                    else
                    {
                        // TODO: move to next location, defend, retreat, etc.
                    }
                }
            }

            return actions;
        }

        IEnumerable<SC2APIProtocol.Action> InjectLarva(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var data in InjectData.Where(d => d.Queen != null))
            {
                if (!data.Hatchery.Unit.BuffIds.Contains((uint)Buffs.QUEENSPAWNLARVATIMER) && data.Queen.UnitCalculation.Unit.Energy >= 25)
                {
                    var action = data.Queen.Order(frame, Abilities.EFFECT_INJECTLARVA, targetTag: data.Hatchery.Unit.Tag);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
                else
                {
                    // TODO: defend, or get in position to spawn larva
                }
            }

            return actions;
        }

        private void UpdatData(int frame)
        {
            foreach (var hatchery in ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.BuildProgress == 1 && u.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
            {
                var existing = InjectData.FirstOrDefault(d => d.Hatchery.Unit.Tag == hatchery.Unit.Tag);
                if (existing == null)
                {
                    InjectData.Add(new InjectData { Hatchery = hatchery });
                }
                else
                {
                    existing.Hatchery = hatchery;
                    if (existing.Queen == null && CreepSpreaders.Count() > 0)
                    {
                        var closest = CreepSpreaders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, existing.Hatchery.Position)).FirstOrDefault();
                        if (closest != null)
                        {
                            CreepSpreaders.Remove(closest);
                            closest.UnitRole = UnitRole.SpawnLarva;
                            existing.Queen = closest;
                        }
                    }
                }             
            }

            RemoveDeadData(frame);
        }


        private void RemoveDeadData(int frame)
        {
            foreach (var data in InjectData)
            {
                if (data.Queen != null && data.Queen.FrameFirstSeen != frame)
                {
                    data.Queen = UnitCommanders.FirstOrDefault(u => u.UnitCalculation.Unit.Tag == data.Queen.UnitCalculation.Unit.Tag);
                }
            }

            foreach (var data in InjectData.Where(d => d.Hatchery.FrameLastSeen != frame))
            {
                if (data.Queen != null)
                {
                    data.Queen.UnitRole = UnitRole.None;
                    data.Queen.Claimed = false;
                    UnitCommanders.RemoveAll(u => u.UnitCalculation.Unit.Tag == data.Queen.UnitCalculation.Unit.Tag);
                }
            }
            InjectData.RemoveAll(d => d.Hatchery.FrameLastSeen != frame);
            CreepSpreaders.RemoveAll(d => d.UnitCalculation.FrameLastSeen != frame);
            UnitCommanders.RemoveAll(u => u.UnitCalculation.FrameLastSeen != frame);
        }
    }
}
