using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class WallOffTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        public IBuildingPlacement WallOffPlacement { get; set; }

        public List<Point2D> PlacementPoints;

        public WallOffTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, ActiveUnitData activeUnitData, MacroData macroData, IBuildingPlacement wallOffPlacement, bool enabled, float priority)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            Priority = priority;
            ActiveUnitData = activeUnitData;
            MacroData = macroData;
            WallOffPlacement = wallOffPlacement;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            PlacementPoints = new List<Point2D>();
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            return;
                        }
                    }
                }
            }
        }

        // TODO: make BuildOptions for partial wall, or full wall, and build buildings at the wall until that is fulfilled
        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (TargetingData.ForwardDefenseWallOffPoints == null)
            {
                return commands;
            }

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints) // TODO: wall with gap, make sure all but one of the wall off points has a building touching, make sure placement doesn't block that, pass in that spot that can't be touched to findplacement method
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    // build a pylon there
                    foreach (var commander in UnitCommanders)
                    {
                        //var placement = WallOffPlacement.FindPlacement(TargetingData.ForwardDefenseWallOffPoints, 3.75f, 0);
                        Point2D placement = null;
                        if (placement != null)
                        {
                            if (MacroData.Minerals < 100)
                            {
                                var action = commander.Order(frame, Abilities.MOVE, placement);
                                if (action != null)
                                {
                                    commands.AddRange(action);
                                }
                            }
                            else
                            {
                                if (!PlacementPoints.Any(p => p.X == placement.X && p.Y == placement.Y))
                                {
                                    PlacementPoints.Add(placement);
                                }

                                var action = commander.Order(frame, Abilities.BUILD_PYLON, placement);
                                if (action != null)
                                {
                                    commands.AddRange(action);
                                }
                            }

                            return commands;
                        }
                    }
                }
            }

            return commands;
        }
    }
}
