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
        WallOffPlacement WallOffPlacement;

        public List<Point2D> PlacementPoints;

        public WallOffTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, ActiveUnitData activeUnitData, MacroData macroData, WallOffPlacement wallOffPlacement, bool enabled, float priority)
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

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    // build a pylon there
                    foreach (var commander in UnitCommanders)
                    {
                        var placement = WallOffPlacement.FindPylonPlacement(point, 3.75f, 0);
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
