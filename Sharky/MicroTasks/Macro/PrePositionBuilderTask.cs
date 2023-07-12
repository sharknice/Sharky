using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Macro
{
    public class PrePositionBuilderTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        MicroTaskData MicroTaskData;

        public Point2D BuildPosition { get; set; }

        int LastSendFrame;
        bool started;

        public PrePositionBuilderTask(DefaultSharkyBot defaultSharkyBot, float priority)
        {
            Enabled = false;
            Priority = priority;

            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;

            UnitCommanders = new List<UnitCommander>();
            LastSendFrame = -1000;
            started = false;
        }

        public void SendBuilder(Point2D buildPoint, int frame)
        {
            if (BuildPosition == null || (BuildPosition.X != buildPoint.X && BuildPosition.Y != buildPoint.Y) || frame - LastSendFrame > 250) // only do this every ~10 seconds
            {
                BuildPosition = buildPoint;
                LastSendFrame = frame;
                if (!Enabled)
                {
                    Enable();
                }
            }
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < 1 && BuildPosition != null)
            {
                if (started)
                {
                    Disable();
                    return;
                }
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, BuildPosition.ToVector2())).ThenBy(c => DistanceToResourceCenter(c)))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        commander.Value.UnitRole = UnitRole.PreBuild;
                        commander.Value.Claimed = true;
                        foreach(var task in MicroTaskData)
                        {
                            task.Value.StealUnit(commander.Value);
                        }
                        UnitCommanders.Add(commander.Value);
                        started = true;
                        return;
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            bool done = false;

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitRole != UnitRole.PreBuild)
                {
                    done = true;
                }
                else
                {
                    if (commander.UnitCalculation.Unit.Orders.Any(o => (o.AbilityId == (uint)Abilities.HARVEST_GATHER_DRONE || o.AbilityId == (uint)Abilities.HARVEST_GATHER_PROBE || o.AbilityId == (uint)Abilities.HARVEST_GATHER_SCV) && commander.UnitCalculation.Unit.Orders.Count() > 1))
                    {
                        actions.AddRange(commander.Order(frame, Abilities.STOP));
                        continue;
                    }

                    var enemyWorker = commander.UnitCalculation.NearbyEnemies.Take(25).FirstOrDefault(e => e.UnitClassifications.Contains(UnitClassification.Worker) && e.FrameLastSeen == frame);
                    if (enemyWorker != null)
                    {
                        var attack = commander.Order(frame, Abilities.ATTACK, targetTag: enemyWorker.Unit.Tag);
                        if (attack != null)
                        {
                            actions.AddRange(attack);
                            commander.UnitRole = UnitRole.Attack;
                            continue;
                        }
                    }

                    var action = commander.Order(frame, Abilities.MOVE, BuildPosition);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
            if (done)
            {
                Disable();
            }

            return actions;
        }

        public override void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
                commander.UnitRole = UnitRole.None;
            }
            UnitCommanders.Clear();

            Enabled = false;
        }

        float DistanceToResourceCenter(KeyValuePair<ulong, UnitCommander> commander)
        {
            var resourceCenter = commander.Value.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            if (resourceCenter != null)
            {
                return Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, resourceCenter.Position);
            }
            return 0;
        }

        public override void Enable()
        {
            started = false;
            base.Enable();
        }
    }
}
