using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class SaveLiftableBuildingTask : MicroTask
    {
        EnemyData EnemyData;
        BaseData BaseData;

        IBuildingPlacement BuildingPlacement;

        public SaveLiftableBuildingTask(DefaultSharkyBot defaultSharkyBot, IBuildingPlacement buildingPlacement, float priority, bool enabled = true)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            BaseData = defaultSharkyBot.BaseData;

            BuildingPlacement = buildingPlacement;

            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>(); // TODO: add this task to the defaultsharkybot
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                foreach (var building in commanders.Where(u => !u.Value.Claimed && u.Value.UnitCalculation.Unit.BuildProgress == 1 && u.Value.UnitCalculation.Unit.Health < u.Value.UnitCalculation.Unit.HealthMax/2 
                    && (u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT)))
                {
                    if (building.Value.UnitCalculation.Unit.Health < building.Value.UnitCalculation.PreviousUnit.Health)
                    {
                        building.Value.Claimed = true;
                        building.Value.UnitRole = UnitRole.Repair;
                        UnitCommanders.Add(building.Value);
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                RemoveFinishedCommanders(frame);

                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
                    {
                        if (!commander.UnitCalculation.Unit.IsFlying)
                        {
                            var action = commander.Order(frame, Abilities.CANCEL_LAST);
                            if (action != null) { actions.AddRange(action); }

                            action = commander.Order(frame, Abilities.LIFT, queue: true);
                            if (action != null) { actions.AddRange(action); }
                        }
                        else
                        {
                            var safeBase = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.Health == b.ResourceCenter.HealthMax).FirstOrDefault();
                            if (safeBase != null)
                            {
                                var action = commander.Order(frame, Abilities.MOVE, safeBase.Location);
                                if (action != null) { actions.AddRange(action); }
                            }
                        }
                    }
                    else
                    {
                        if (commander.UnitCalculation.Unit.IsFlying)
                        {
                            if (!commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId > 0 && o.AbilityId != (uint)Abilities.MOVE))
                            {
                                var location = BuildingPlacement.FindPlacement(new Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y }, (UnitTypes)commander.UnitCalculation.Unit.UnitType, (int)(commander.UnitCalculation.Unit.Radius * 2));

                                if (location != null)
                                {
                                    var action = commander.Order(frame, Abilities.LAND, location);
                                    if (action != null) { actions.AddRange(action); }
                                }
                            }
                        }
                    }
                }
            }

            return actions;
        }

        private void RemoveFinishedCommanders(int frame)
        {
            var doneList = UnitCommanders.Where(c => c.UnitCalculation.Unit.Health > c.UnitCalculation.Unit.HealthMax / 2 && !c.UnitCalculation.Unit.IsFlying);
            foreach (var commander in doneList)
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
                UnitCommanders.Remove(commander);
            }
        }
    }
}
