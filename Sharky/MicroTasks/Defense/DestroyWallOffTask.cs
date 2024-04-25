namespace Sharky.MicroTasks
{
    public class DestroyWallOffTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;

        public List<Point2D> WallPoints;
        public bool Ended { get; set; }

        public DestroyWallOffTask(ActiveUnitData activeUnitData, bool enabled, float priority)
        {
            ActiveUnitData = activeUnitData;
            Priority = priority;
            Enabled = enabled;

            UnitCommanders = new List<UnitCommander>();
            Ended = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < 10 && WallPoints != null)
            {
                var point = WallPoints.FirstOrDefault();
                if (point != null)
                {
                    var vector = new Vector2(point.X, point.Y);
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)).OrderBy(c => Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position)))
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.WallOff;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() >= 10)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public override void Enable()
        {
            Enabled = true;
            Ended = false;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (WallPoints != null)
            {
                var buildings = ActiveUnitData.Commanders.Where(u => u.Value.UnitCalculation.Attributes.Contains(SC2Attribute.Structure) && WallPoints.Any(point => Vector2.DistanceSquared(new Vector2(point.X, point.Y), u.Value.UnitCalculation.Position) < 1)).Select(b => b.Value);
                foreach (var building in buildings)
                {
                    building.UnitRole = UnitRole.Die;
                }

                if (buildings.Count() == 0)
                {
                    Ended = true;
                    Disable();
                }

                var attacked = false;

                foreach (var building in buildings)
                {
                    if (building != null)
                    {
                        if (building.UnitCalculation.Unit.BuildProgress < 1)
                        {
                            var action = building.Order(frame, Abilities.CANCEL);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                        else
                        {
                            if (!attacked)
                            {
                                var command = new ActionRawUnitCommand();
                                foreach (var commander in UnitCommanders)
                                {
                                    command.UnitTags.Add(commander.UnitCalculation.Unit.Tag);
                                }
                                command.AbilityId = (int)Abilities.ATTACK;
                                command.TargetUnitTag = building.UnitCalculation.Unit.Tag; // TODO: mark building for death so it doesn't get healed by shield batteries

                                var action = new SC2APIProtocol.Action
                                {
                                    ActionRaw = new ActionRaw
                                    {
                                        UnitCommand = command
                                    }
                                };
                                actions.Add(action);
                                attacked = true;
                            }
                        }
                    }
                }
            }

            return actions;
        }
    }
}
