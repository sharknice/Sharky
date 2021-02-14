using SC2APIProtocol;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < 10 && WallPoints != null)
            {
                var point = WallPoints.FirstOrDefault();
                if (point != null)
                {
                    var vector = new Vector2(point.X, point.Y);
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit)).OrderBy(c => Vector2.DistanceSquared(vector, new Vector2(c.Value.UnitCalculation.Unit.Pos.X, c.Value.UnitCalculation.Unit.Pos.Y))))
                    {
                        commander.Value.Claimed = true;
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
                var buildings = ActiveUnitData.Commanders.Where(u => u.Value.UnitCalculation.Attributes.Contains(Attribute.Structure) && WallPoints.Any(point => Vector2.DistanceSquared(new Vector2(point.X, point.Y), new Vector2(u.Value.UnitCalculation.Unit.Pos.X, u.Value.UnitCalculation.Unit.Pos.Y)) < 1)).Select(b => b.Value);

                if (buildings.Count() == 0)
                {
                    Ended = true;
                    Disable();
                }

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
                            var command = new ActionRawUnitCommand();
                            foreach (var commander in UnitCommanders)
                            {
                                command.UnitTags.Add(commander.UnitCalculation.Unit.Tag);
                            }
                            command.AbilityId = (int)Abilities.ATTACK;
                            command.TargetUnitTag = building.UnitCalculation.Unit.Tag;

                            var action = new SC2APIProtocol.Action
                            {
                                ActionRaw = new ActionRaw
                                {
                                    UnitCommand = command
                                }
                            };
                            actions.Add(action);
                        }
                    }
                }
            }

            return actions;
        }
    }
}
