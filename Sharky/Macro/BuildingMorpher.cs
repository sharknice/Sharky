namespace Sharky.Macro
{
    public class BuildingMorpher
    {
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        Morpher Morpher;

        public BuildingMorpher(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            Morpher = defaultSharkyBot.Morpher;
        }

        public List<SC2Action> MorphBuildings()
        {
            var commands = new List<SC2Action>();

            foreach (var unit in MacroData.Morph)
            {
                if (unit.Value)
                {
                    var unitData = SharkyUnitData.MorphData[unit.Key];
                    var command = Morpher.MorphBuilding(MacroData, unitData);
                    if (command != null)
                    {
                        commands.AddRange(command);
                        return commands;
                    }
                }
            }

            return commands;
        }
    }
}
