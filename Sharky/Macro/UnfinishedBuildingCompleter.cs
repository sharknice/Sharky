namespace Sharky.Macro
{
    public class UnfinishedBuildingCompleter
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;

        public UnfinishedBuildingCompleter(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MacroData = defaultSharkyBot.MacroData;
        }

        public List<SC2Action> SendScvToFinishIncompleteBuildings()
        {
            var commands = new List<SC2Action>();

            foreach (var building in ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.Unit.BuildProgress < 1 && c.Value.UnitCalculation.Unit.BuildProgress > 0 && c.Value.UnitCalculation.Attributes.Contains(SC2Attribute.Structure) && c.Value.UnitCalculation.Unit.BuildProgress == c.Value.UnitCalculation.PreviousUnit.BuildProgress))
            {
                if (building.Value.UnitCalculation.EnemiesInRangeOf.Count() > building.Value.UnitCalculation.NearbyAllies.Count(a => a.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) || a.UnitClassifications.HasFlag(UnitClassification.DefensiveStructure)))
                {
                    continue; // do not send scvs to suicide
                }

                var scvs = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
                var buildingScv = scvs.FirstOrDefault(c => c.UnitCalculation.Unit.Orders.Any(o => o.TargetUnitTag == building.Key || (o.TargetWorldSpacePos != null && o.TargetWorldSpacePos.X == building.Value.UnitCalculation.Position.X && o.TargetWorldSpacePos.Y == building.Value.UnitCalculation.Position.Y )));
                if (buildingScv == null)
                {
                    var completer = GetWorker(new Point2D { X = building.Value.UnitCalculation.Position.X, Y = building.Value.UnitCalculation.Position.Y });
                    if (completer != null)
                    {
                        completer.UnitRole = UnitRole.Build;
                        var command = completer.Order(MacroData.Frame, Abilities.SMART, targetTag: building.Key);
                        if (command != null)
                        {
                            commands.AddRange(command);
                            return commands;
                        }
                    }
                }
            }

            return commands;
        }

        UnitCommander GetWorker(Point2D location, IEnumerable<UnitCommander> workers = null)
        {
            IEnumerable<UnitCommander> availableWorkers;
            if (workers == null)
            {
                availableWorkers = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.PreBuild || c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))).OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, new Vector2(location.X, location.Y)));
            }
            else
            {
                availableWorkers = workers.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))).OrderBy(p => Vector2.DistanceSquared(p.UnitCalculation.Position, new Vector2(location.X, location.Y)));
            }

            if (availableWorkers.Count() == 0)
            {
                return null;
            }
            else
            {
                var closest = availableWorkers.First();
                var pos = closest.UnitCalculation.Position;
                var distanceSquared = Vector2.DistanceSquared(pos, new Vector2(location.X, location.Y));
                if (distanceSquared > 1000)
                {
                    pos = availableWorkers.First().UnitCalculation.Position;

                    if (Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(location.X, location.Y)) > distanceSquared)
                    {
                        return closest;
                    }
                    else
                    {
                        return availableWorkers.First();
                    }
                }
            }
            return availableWorkers.First();
        }
    }
}
