namespace Sharky.Macro
{
    public class VespeneGasBuilder
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;

        IBuildingBuilder BuildingBuilder;

        public VespeneGasBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingBuilder buildingBuilder)
        {
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BaseData = defaultSharkyBot.BaseData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;

            BuildingBuilder = buildingBuilder;
        }

        public List<SC2APIProtocol.Action> BuildVespeneGas()
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (MacroData.BuildGas && MacroData.Minerals >= 75)
            {
                var unitData = GetGasTypeData();
                var takenGases = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)).Concat(ActiveUnitData.EnemyUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)));
                var orderedGases = ActiveUnitData.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.Worker) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR || o.AbilityId == (uint)Abilities.BUILD_EXTRACTOR || o.AbilityId == (uint)Abilities.BUILD_REFINERY)).Select(u => u.Value.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.BUILD_ASSIMILATOR || o.AbilityId == (uint)Abilities.BUILD_EXTRACTOR || o.AbilityId == (uint)Abilities.BUILD_REFINERY));
                var openGeysers = BaseData.BaseLocations.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress > .9f && b.ResourceCenter.Alliance == Alliance.Self).SelectMany(b => b.VespeneGeysers).Where(g => g.VespeneContents > 0 && !takenGases.Any(t => t.Value.Unit.Pos.X == g.Pos.X && t.Value.Unit.Pos.Y == g.Pos.Y) && !orderedGases.Any(o => o.TargetUnitTag == g.Tag));
                if (openGeysers.Count() > 0)
                {
                    var baseLocation = BuildingBuilder.GetReferenceLocation(TargetingData.SelfMainBasePoint);
                    var closestGyeser = openGeysers.OrderBy(o => Vector2.DistanceSquared(new Vector2(baseLocation.X, baseLocation.Y), new Vector2(o.Pos.X, o.Pos.Y))).FirstOrDefault();
                    if (closestGyeser != null)
                    {
                        var actualGyser = ActiveUnitData.NeutralUnits.Values.FirstOrDefault(g => g.Unit.Pos.X == closestGyeser.Pos.X && g.Unit.Pos.Y == closestGyeser.Pos.Y);
                        if (actualGyser != null)
                        {
                            var command = BuildingBuilder.BuildGas(MacroData, unitData, actualGyser.Unit);
                            if (command != null)
                            {
                                commands.AddRange(command);
                                return commands;
                            }
                        }
                    }

                }
            }

            return commands;
        }

        private BuildingTypeData GetGasTypeData()
        {
            if (MacroData.Race == Race.Protoss)
            {
                return SharkyUnitData.BuildingData[UnitTypes.PROTOSS_ASSIMILATOR];
            }
            else if (MacroData.Race == Race.Terran)
            {
                return SharkyUnitData.BuildingData[UnitTypes.TERRAN_REFINERY];
            }
            else
            {
                return SharkyUnitData.BuildingData[UnitTypes.ZERG_EXTRACTOR];
            }
        }
    }
}
