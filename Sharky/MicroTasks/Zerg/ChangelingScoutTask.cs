using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SC2APIProtocol;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class ChangelingScoutTask : MicroTask
    {
        private BaseData BaseData;
        private MicroTaskData MicroTaskData;
        private System.Random rnd = new();

        public ChangelingScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            BaseData = defaultSharkyBot.BaseData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var changeling in commanders.Values.Where(u => IsChangeling(u.UnitCalculation.Unit.UnitType) && !UnitCommanders.Contains(u)))
            {
                changeling.UnitRole = UnitRole.Scout;
                changeling.Claimed = true;
                MicroTaskData.StealCommanderFromAllTasks(changeling);
                UnitCommanders.Add(changeling);
            }
        }

        private bool IsChangeling(uint type)
        {
            return type >= (uint)UnitTypes.ZERG_CHANGELING && type <= (uint)UnitTypes.ZERG_CHANGELINGZERGLING;
        }

        public override IEnumerable<Action> PerformActions(int frame)
        {
            var commands = new List<Action>();
            foreach (var commander in UnitCommanders)
            {
                if (!commander.UnitCalculation.Unit.Orders.Any())
                {
                    var b = BaseData.EnemyBaseLocations.Select(x => x.Location).ElementAtOrDefault(rnd.Next(BaseData.EnemyBaseLocations.Count));

                    if (b is not null)
                    {
                        commands.AddRange(commander.Order(frame, Abilities.MOVE, b));
                    }
                }
            }

            return commands;
        }
    }
}
