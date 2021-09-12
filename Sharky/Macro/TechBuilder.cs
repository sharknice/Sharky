using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Macro
{
    public class TechBuilder
    {
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        BuildOptions BuildOptions;

        IBuildingBuilder BuildingBuilder;

        bool SkipTech;

        public TechBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingBuilder buildingBuilder)
        {
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;

            BuildingBuilder = buildingBuilder;
        }

        public List<Action> BuildTechBuildings()
        {
            var commands = new List<Action>();
            if (SkipTech)
            {
                SkipTech = false;
                return commands;
            }
            var begin = System.DateTime.UtcNow;

            foreach (var unit in MacroData.BuildTech)
            {
                if (unit.Value)
                {
                    var unitData = SharkyUnitData.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData, wallOffType: BuildOptions.WallOffType);
                    if (command != null)
                    {
                        commands.AddRange(command);
                        return commands;
                    }
                }
            }

            var endTime = (System.DateTime.UtcNow - begin).TotalMilliseconds;
            if (endTime > 1)
            {
                SkipTech = true;
            }

            return commands;
        }
    }
}
