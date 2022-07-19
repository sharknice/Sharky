using Sharky.DefaultBot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.MacroServices
{
    /// <summary>
    /// Class for requesting upgrade cancelling
    /// </summary>
    public class UpgradeRequestCancellingService
    {
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;

        private HashSet<Upgrades> CancelRequests = new();

        public UpgradeRequestCancellingService(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
        }

        /// <summary>
        /// Requests upgrade cancelling.
        /// </summary>
        /// <param name="unitType">Upgrade type to cancel.</param>
        public void RequestCancel(Upgrades upgrade)
        {
            CancelRequests.Add(upgrade);
            MacroData.DesiredUpgrades[upgrade] = false;
        }

        public List<SC2APIProtocol.Action> CancelUpgrades()
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var upgrade in CancelRequests)
            {
                if (!SharkyUnitData.ResearchedUpgrades.Contains((uint)upgrade))
                {
                    var upgradeData = SharkyUnitData.UpgradeData[upgrade];

                    var upgrader = ActiveUnitData.Commanders.Values.FirstOrDefault(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.UnitCalculation.Unit.UnitType) && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (int)upgradeData.Ability));

                    if (upgrader != null)
                    {
                        System.Console.WriteLine($"Cancelling {upgrade} upgrade.");
                        commands.AddRange(upgrader.Order(MacroData.Frame, Abilities.CANCEL_LAST));
                    }
                }
            }

            CancelRequests.Clear();

            return commands;
        }
    }
}
