using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers.Zerg;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class QueenDefendTask : MicroTask
    {
        // Max distance larva queens would go to help defend
        const float maxDistanceInjectingQueen = 25;

        // Max distance any queen would go to help defend
        const float maxDistance = 50;

        EnemyData EnemyData;
        ActiveUnitData ActiveUnitData;
        QueenMicroController QueenMicroController;

        public QueenDefendTask(DefaultSharkyBot defaultSharkyBot, float priority, QueenMicroController queenMicroController, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            QueenMicroController = queenMicroController;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
            }

            bool needsDefend = EnemyData.EnemyAggressivityData.IsHarassing || EnemyData.EnemyAggressivityData.ArmyAggressivity > 0.7f;

            // TODO: better queen splitting decision - try not to use injecting queens if not necessary, use only needed amount of queens to have some for possible multiprong attacks

            if (needsDefend)
            {
                foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !UnitCommanders.Contains(commander.Value)))
                {
                    var enemy = FindNearestEnemyPos(commander.Value);

                    if (enemy != null)
                    {
                        commander.Value.UnitRole = UnitRole.Defend;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                }

                foreach (var commander in UnitCommanders)
                {
                    commander.UnitRole = UnitRole.Defend;
                }
            }
            else
            {
                foreach (var commander in UnitCommanders)
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                }
                UnitCommanders.Clear();
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var toRemove = new List<UnitCommander>();

            foreach (var queen in UnitCommanders)
            {
                {
                    var attackPos = FindNearestEnemyPos(queen);

                    if (attackPos != null)
                    {
                        var action = QueenMicroController.Attack(queen, attackPos, attackPos, null, frame);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                    else
                    {
                        // No target close enough, remove
                        toRemove.Add(queen);
                    }
                }
            }

            foreach (var u in toRemove)
            {
                u.UnitRole = UnitRole.None;
                u.Claimed = false;
                UnitCommanders.Remove(u);
            }

            return actions;
        }

        private Point2D FindNearestEnemyPos(UnitCommander commander)
        {
            // TODO: defend only if the defence in the area is not big enough as we are using also inject queens
            // TODO: back with queen when injured
            var defendDistance = commander.UnitRole == UnitRole.SpawnLarva ? maxDistanceInjectingQueen : maxDistance;

            var enemiesDistances = ActiveUnitData.EnemyUnits.Values
                .Select(u => new { unit = u, distance = u.Position.Distance(commander.UnitCalculation.Position) })
                .Where(u => u.distance < defendDistance && (EnemyData.EnemyAggressivityData.DistanceGrid.GetDist(u.unit.Position.X, u.unit.Position.Y, true, false) <= 14))
                .OrderBy(x => x.distance);

            return enemiesDistances.FirstOrDefault()?.unit.Position.ToPoint2D();
        }
    }
}
