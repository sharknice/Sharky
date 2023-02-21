using Sharky.DefaultBot;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Mining
{
    public class ReaperMiningDefenseTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        MineralWalker MineralWalker;

        UnitCalculation EnemyReaper;

        public ReaperMiningDefenseTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            MineralWalker = defaultSharkyBot.MineralWalker;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg || EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                Disable();
            }

            var commands = new List<SC2APIProtocol.Action>();

            GetEnemyReaper();
          
            commands = DefendAgainstReaper(frame);

            if (EnemyReaper == null)
            {
                UnitCommanders.ForEach(c => c.UnitRole = UnitRole.None);
            }

            UnitCommanders.RemoveAll(c => c.UnitRole != UnitRole.ChaseReaper && c.UnitRole != UnitRole.RunAway);

            return commands;
        }

        private List<SC2APIProtocol.Action> DefendAgainstReaper(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyReaper != null && UnitCommanders.Count() < 3)
            {
                ClaimDefenders();
            }

            foreach (var commander in UnitCommanders)
            {
                var healthRequired = 15;
                if (UnitCommanders.Count(c => c.UnitRole == UnitRole.ChaseReaper) < 3)
                {
                    healthRequired = 25;
                }
                if (EnemyReaper == null || commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield <= healthRequired || !commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
                {
                    commander.UnitRole = UnitRole.RunAway;
                    List<SC2APIProtocol.Action> action;
                    if (MineralWalker.MineralWalkHome(commander, frame, out action))
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, targetTag: EnemyReaper.Unit.Tag);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }

        private void ClaimDefenders()
        {
            var worker = EnemyReaper.NearbyEnemies.FirstOrDefault(e => e.UnitClassifications.Contains(UnitClassification.Worker) && 
                e.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter) &&
                ActiveUnitData.Commanders.ContainsKey(e.Unit.Tag) && ActiveUnitData.Commanders[e.Unit.Tag].UnitRole != UnitRole.ChaseReaper && 
                e.Unit.Health + e.Unit.Shield >= 40));

            if (worker != null)
            {
                ActiveUnitData.Commanders[worker.Unit.Tag].UnitRole = UnitRole.ChaseReaper;
                UnitCommanders.Add(ActiveUnitData.Commanders[worker.Unit.Tag]);
            }
        }

        private void GetEnemyReaper()
        {
            EnemyReaper = ActiveUnitData.EnemyUnits.Values.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER && e.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ResourceCenter)) && !e.NearbyAllies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && !e.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !e.Unit.IsFlying));
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                if (EnemyReaper != null && EnemyReaper.Unit.Tag == tag)
                {
                    EnemyReaper = null;
                }
            }
            base.RemoveDeadUnits(deadUnits);
        }
    }
}
