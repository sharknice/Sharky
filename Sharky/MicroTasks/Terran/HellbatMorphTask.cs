using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class HellbatMorphTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;

        public HellbatMorphTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            var hellions = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION);
            if (hellions.Count() > 0)
            {
                var command = new ActionRawUnitCommand();
                command.UnitTags.AddRange(hellions.Select(h => h.Unit.Tag));
                command.AbilityId = (int)Abilities.MORPH_HELLBAT;

                var action = new SC2APIProtocol.Action
                {
                    ActionRaw = new ActionRaw
                    {
                        UnitCommand = command
                    }
                };

                commands.Add(action);
            }

            return commands;
        }
    }
}
