using Sharky.Builds;
using Sharky.DefaultBot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class QueenInjectTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        UnitCountService UnitCountService;
        BuildOptions BuildOptions;

        List<InjectData> HatcheryQueenPairing = new List<InjectData>();

        public QueenInjectTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;

            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            BuildOptions = defaultSharkyBot.BuildOptions;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (BuildOptions.ZergBuildOptions.SecondQueenPreferCreepOnBorn && UnitCommanders.Count == 1 && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_CREEPTUMORBURROWED) == 0)
                return;

            foreach (var entry in HatcheryQueenPairing)
            {
                if (entry.Queen == null)
                {
                    var closest = commanders.Values.Where(commander => (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) 
                    && (!commander.Claimed || commander.UnitRole == UnitRole.SpreadCreep))
                    .OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, entry.Hatchery.Position)).FirstOrDefault();
                    
                    if (closest != null)
                    {
                        closest.Claimed = true;
                        closest.UnitRole = UnitRole.SpawnLarva;
                        UnitCommanders.Add(closest);
                        entry.Queen = closest;
                    }
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

            UpdateData(frame);

            return InjectLarva(frame);
        }

        IEnumerable<SC2APIProtocol.Action> InjectLarva(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var entry in HatcheryQueenPairing)
            {
                if (entry.Queen != null && !entry.Hatchery.Unit.BuffIds.Contains((uint)Buffs.QUEENSPAWNLARVATIMER) && entry.Queen.UnitCalculation.Unit.Energy >= 25)
                {
                    var action = entry.Queen.Order(frame, Abilities.EFFECT_INJECTLARVA, targetTag: entry.Hatchery.Unit.Tag);
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

        private void UpdateData(int frame)
        {
            foreach (var hatchery in ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.BuildProgress > 0.9 && u.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
            {
                var existing = HatcheryQueenPairing.FirstOrDefault(d => d.Hatchery.Unit.Tag == hatchery.Unit.Tag);
                if (existing == null)
                {
                    HatcheryQueenPairing.Add(new InjectData { Hatchery = hatchery });
                }
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            base.RemoveDeadUnits(deadUnits);

            foreach (var entry in HatcheryQueenPairing)
            {
                if (deadUnits.Contains(entry.Hatchery.Unit.Tag))
                {
                    entry.Hatchery = null;
                }

                if (entry.Queen != null && deadUnits.Contains(entry.Queen.UnitCalculation.Unit.Tag))
                {
                    entry.Queen = null;
                }
            }

            HatcheryQueenPairing.RemoveAll(x => x.Hatchery == null);
        }
    }
}
