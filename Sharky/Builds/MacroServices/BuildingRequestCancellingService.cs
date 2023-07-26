namespace Sharky.Builds.MacroServices
{
    /// <summary>
    /// Class for requesting building cancelling
    /// </summary>
    public class BuildingRequestCancellingService
    {
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        UnitCountService UnitCountService;

        private Dictionary<UnitTypes, int> CancelRequests = new Dictionary<UnitTypes, int>();

        public BuildingRequestCancellingService(ActiveUnitData activeUnitData, MacroData macroData, UnitCountService unitCountService)
        {
            ActiveUnitData = activeUnitData;
            MacroData = macroData;
            UnitCountService = unitCountService;
        }

        /// <summary>
        /// Requests building cancelling.
        /// </summary>
        /// <param name="unitType">Building unit type to cancel building.</param>
        /// <param name="maxCount">Max unit count to preserve. Set to zero if you want to cancel all buildings in progress of this type.</param>
        public void RequestCancel(UnitTypes unitType, int maxCount = 0)
        {
            CancelRequests[unitType] = maxCount;
        }

        public List<SC2APIProtocol.Action> CancelBuildings()
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var request in CancelRequests)
            {
                int completed = UnitCountService.Completed(request.Key);
                int building = ActiveUnitData.SelfUnits.Values.Count(u => (u.Unit.UnitType == (uint)request.Key) && (u.Unit.BuildProgress < 1.0f));

                if (building > 0 && (completed + building) > request.Value)
                {
                    int toCancel = (completed + building) - request.Value;

                    foreach (var commander in ActiveUnitData.Commanders.OrderBy(c => c.Value.UnitCalculation.Unit.BuildProgress))
                    {
                        if (toCancel <= 0)
                            break;

                        if (commander.Value.UnitCalculation.Unit.UnitType == (int)request.Key && commander.Value.UnitCalculation.Unit.BuildProgress < 1.0f)
                        {
                            System.Console.WriteLine($"Cancelling {(UnitTypes)commander.Value.UnitCalculation.Unit.UnitType} which was {(commander.Value.UnitCalculation.Unit.BuildProgress * 100.0f):F2}% done.");
                            commands.AddRange(commander.Value.Order(MacroData.Frame, Abilities.CANCEL_BUILDINPROGRESS));
                            toCancel--;
                        }
                    }
                }
            }

            CancelRequests.Clear();

            return commands;
        }
    }
}
