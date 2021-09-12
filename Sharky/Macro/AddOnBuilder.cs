using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Macro
{
    public class AddOnBuilder
    {
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;

        IBuildingBuilder BuildingBuilder;

        bool SkipAddons;

        public AddOnBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingBuilder buildingBuilder)
        {
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            BuildingBuilder = buildingBuilder;
        }

        public List<Action> BuildAddOns()
        {
            var commands = new List<Action>();
            if (SkipAddons)
            {
                SkipAddons = false;
                return commands;
            }
            var begin = System.DateTime.UtcNow;

            foreach (var unit in MacroData.BuildAddOns)
            {
                if (unit.Value)
                {
                    var unitData = SharkyUnitData.AddOnData[unit.Key];
                    var command = BuildingBuilder.BuildAddOn(MacroData, unitData);
                    if (command != null)
                    {
                        commands.AddRange(command);
                        continue;
                    }
                }
            }

            var endTime = (System.DateTime.UtcNow - begin).TotalMilliseconds;
            if (endTime > 1)
            {
                SkipAddons = true;
            }

            return commands;
        }
    }
}
