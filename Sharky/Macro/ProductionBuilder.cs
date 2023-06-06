using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sharky.Macro
{
    public class ProductionBuilder
    {
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        BuildOptions BuildOptions;

        IBuildingBuilder BuildingBuilder;

        bool SkipProduction;

        public ProductionBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingBuilder buildingBuilder)
        {
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;

            BuildingBuilder = buildingBuilder;
        }

        public List<Action> BuildProductionBuildings()
        {
            var commands = new List<Action>();
            if (SkipProduction)
            {
                SkipProduction = false;
                return commands;
            }
            var begin = Stopwatch.GetTimestamp();

            foreach (var unit in MacroData.BuildProduction)
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

            var endTime = (Stopwatch.GetTimestamp() - begin) / (double)Stopwatch.Frequency * 1000.0;
            if (endTime > 1)
            {
                SkipProduction = true;
            }

            return commands;
        }
    }
}
