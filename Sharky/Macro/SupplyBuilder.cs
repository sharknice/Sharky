namespace Sharky.Macro
{
    public class SupplyBuilder
    {
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        BuildOptions BuildOptions;
        BaseData BaseData;

        IBuildingBuilder BuildingBuilder;

        bool SkipSupply;

        public SupplyBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingBuilder buildingBuilder)
        {
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            BaseData = defaultSharkyBot.BaseData;

            BuildingBuilder = buildingBuilder;
        }

        public List<SC2Action> BuildSupply()
        {
            var commands = new List<SC2Action>();
            if (SkipSupply)
            {
                SkipSupply = false;
                return commands;
            }

            var begin = Stopwatch.GetTimestamp();

            if (MacroData.BuildPylon)
            {
                var requireSameHeight = BaseData.SelfBases.Count == 1;
                var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
                var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData, wallOffType: BuildOptions.WallOffType, requireSameHeight: requireSameHeight);
                if (command != null)
                {
                    commands.AddRange(command);
                    return commands;
                }
            }

            if (MacroData.BuildSupplyDepot)
            {
                var unitData = SharkyUnitData.BuildingData[UnitTypes.TERRAN_SUPPLYDEPOT];
                var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.TERRAN_SUPPLYDEPOT, unitData, wallOffType: BuildOptions.WallOffType);
                if (command != null)
                {
                    commands.AddRange(command);
                    return commands;
                }
            }

            if (MacroData.BuildOverlord)
            {
                MacroData.BuildUnits[UnitTypes.ZERG_OVERLORD] = true;
            }

            var endTime = (Stopwatch.GetTimestamp() - begin) / (double)Stopwatch.Frequency * 1000.0;
            if (endTime > 1)
            {
                SkipSupply = true;
            }

            return commands;
        }
    }
}
