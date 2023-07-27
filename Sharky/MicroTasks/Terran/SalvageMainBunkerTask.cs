namespace Sharky.MicroTasks
{
    public class SalvageMainBunkerTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        MapDataService MapDataService;
        TargetingData TargetingData;

        public SalvageMainBunkerTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MapDataService = defaultSharkyBot.MapDataService;
            TargetingData = defaultSharkyBot.TargetingData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            var mainBunkers = ActiveUnitData.Commanders.Values.Where(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && !u.UnitCalculation.NearbyEnemies.Any() && MapDataService.MapHeight(u.UnitCalculation.Position) == MapDataService.MapHeight(TargetingData.SelfMainBasePoint) &&  Vector2.DistanceSquared(u.UnitCalculation.Position, TargetingData.SelfMainBasePoint.ToVector2()) < 225);
            foreach (var bunker in mainBunkers)
            {
                if (bunker.UnitCalculation.Unit.Passengers.Any())
                {
                    commands.AddRange(bunker.Order(frame, Abilities.UNLOADALL));
                }
                else
                {
                    commands.AddRange(bunker.Order(frame, Abilities.EFFECT_SALVAGE));
                }
            }

            return commands;
        }
    }
}
