namespace Sharky.Macro
{
    public class UpgradeResearcher
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        CameraManager CameraManager;

        public UpgradeResearcher(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            CameraManager = defaultSharkyBot.CameraManager;
        }

        public List<SC2Action> ResearchUpgrades()
        {
            var commands = new List<SC2Action>();

            foreach (var upgrade in MacroData.DesiredUpgrades)
            {
                if (upgrade.Value && !SharkyUnitData.ResearchedUpgrades.Contains((uint)upgrade.Key))
                {
                    var upgradeData = SharkyUnitData.UpgradeData[upgrade.Key];

                    if (!ActiveUnitData.Commanders.Any(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (int)upgradeData.Ability)))
                    {
                        var building = ActiveUnitData.Commanders.Where(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.LastOrderFrame != MacroData.Frame);
                        if (building.Any())
                        {
                            if (upgradeData.Minerals <= MacroData.Minerals && upgradeData.Gas <= MacroData.VespeneGas)
                            {
                                CameraManager.SetCamera(building.First().Value.UnitCalculation.Position);
                                commands.AddRange(building.First().Value.Order(MacroData.Frame, upgradeData.Ability));
                            }
                        }
                    }
                }
            }

            return commands;
        }
    }
}
