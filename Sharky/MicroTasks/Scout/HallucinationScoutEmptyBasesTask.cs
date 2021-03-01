using SC2APIProtocol;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class HallucinationScoutEmptyBasesTask : MicroTask
    {
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;

        bool started { get; set; }
        List<Point2D> ScoutLocations { get; set; }
        int ScoutLocationIndex { get; set; }

        public HallucinationScoutEmptyBasesTask(BaseData baseData, ActiveUnitData activeUnitData, bool enabled, float priority)
        {
            BaseData = baseData;
            Priority = priority;
            ActiveUnitData = activeUnitData;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < 2)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                if (!UnitCommanders.Any(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY))
                {
                    foreach (var commander in commanders)
                    {
                        if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
                        {
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            break;
                        }
                    }
                }
                if (!UnitCommanders.Any(c => c.UnitCalculation.Unit.IsHallucination && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX))
                {
                    foreach (var commander in commanders)
                    {
                        if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.IsHallucination && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX)
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            break;
                        }
                    }
                }
            }
            else
            {
                started = true;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (ScoutLocations == null)
            {
                GetScoutLocations();
            }

            var commands = new List<SC2APIProtocol.Action>();

            if (!UnitCommanders.Any(c => c.UnitCalculation.Unit.IsHallucination && c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX))
            {
                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY && commander.UnitCalculation.Unit.Energy >= 75)
                    {
                        var action = commander.Order(frame, Abilities.HALLUCINATION_PHOENIX);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
            }
            else
            {
                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX)
                    {
                        if (Vector2.DistanceSquared(new Vector2(ScoutLocations[ScoutLocationIndex].X, ScoutLocations[ScoutLocationIndex].Y), commander.UnitCalculation.Position) < 2)
                        {
                            ScoutLocationIndex++;
                            if (ScoutLocationIndex >= ScoutLocations.Count())
                            {
                                ScoutLocationIndex = 0;
                            }
                        }
                        else
                        {
                            var action = commander.Order(frame, Abilities.MOVE, ScoutLocations[ScoutLocationIndex]);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                }
            }

            return commands;
        }

        private void GetScoutLocations()
        {
            ScoutLocations = new List<Point2D>();

            foreach (var baseLocation in BaseData.EnemyBaseLocations.Where(b => !ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && Vector2.DistanceSquared(e.Value.Position, new Vector2(b.Location.X, b.Location.Y)) < 50) && !ActiveUnitData.SelfUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) && Vector2.DistanceSquared(e.Value.Position, new Vector2(b.Location.X, b.Location.Y)) < 50)))
            {
                ScoutLocations.Add(baseLocation.MineralLineLocation);
            }
            if (ScoutLocations.Count() == 0)
            {
                ScoutLocations.AddRange(BaseData.EnemyBaseLocations.Select(b => b.MineralLineLocation));
            }
            ScoutLocationIndex = 0;
        }

        public override void Enable()
        {
            started = false;
            Enabled = true;
        }
    }
}
