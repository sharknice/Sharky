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

        public List<SC2Action> BuildAddOns()
        {
            var commands = new List<SC2Action>();
            if (SkipAddons)
            {
                SkipAddons = false;
                return commands;
            }
            var begin = Stopwatch.GetTimestamp();

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

            var endTime = (Stopwatch.GetTimestamp() - begin) / (double)Stopwatch.Frequency * 1000.0;
            if (endTime > 1)
            {
                SkipAddons = true;
            }

            return commands;
        }
    }
}
