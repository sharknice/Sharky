using SC2APIProtocol;
using Sharky.MicroControllers;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Attack
{
    public class ArmySplitter
    {
        AttackData AttackData;
        TargetingData TargetingData;

        DefenseService DefenseService;

        IMicroController MicroController;

        float LastSplitFrame;

        List<ArmySplits> ArmySplits;
        List<UnitCommander> AvailableCommanders;

        public ArmySplitter(AttackData attackData, TargetingData targetingData, DefenseService defenseService, IMicroController microController)
        {
            AttackData = attackData;
            TargetingData = targetingData;

            DefenseService = defenseService;

            MicroController = microController;

            LastSplitFrame = -1000;
        }

        public List<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders, bool defendToDeath)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var winnableDefense = false;

            if (LastSplitFrame + 25 < frame)
            {
                ReSplitArmy(frame, closerEnemies, attackPoint, unitCommanders);
                LastSplitFrame = frame;
            }

            foreach (var split in ArmySplits)
            {
                if (split.SelfGroup.Count() > 0)
                {
                    var groupVectors = split.SelfGroup.Select(u => u.UnitCalculation.Position);
                    var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                    var defensePoint = new Point2D { X = split.EnemyGroup.FirstOrDefault().Unit.Pos.X, Y = split.EnemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(split.SelfGroup, defensePoint, TargetingData.ForwardDefensePoint, groupPoint, frame));

                    winnableDefense = true;
                }
            }

            if (AvailableCommanders.Count() > 0)
            {
                var groupVectors = AvailableCommanders.Select(u => u.UnitCalculation.Position);
                var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(AvailableCommanders, attackPoint, TargetingData.ForwardDefensePoint, groupPoint, frame));
                }
                else
                {
                    if (winnableDefense || defendToDeath)
                    {
                        actions.AddRange(MicroController.Attack(AvailableCommanders, new Point2D { X = closerEnemies.FirstOrDefault().Unit.Pos.X, Y = closerEnemies.FirstOrDefault().Unit.Pos.Y }, TargetingData.ForwardDefensePoint, groupPoint, frame));
                    }
                    else
                    {
                        actions.AddRange(MicroController.Retreat(AvailableCommanders, TargetingData.MainDefensePoint, groupPoint, frame));
                    }
                }
            }

            return actions;
        }

        void ReSplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, IEnumerable<UnitCommander> unitCommanders)
        {
            ArmySplits = new List<ArmySplits>();
            var enemyGroups = DefenseService.GetEnemyGroups(closerEnemies);
            AvailableCommanders = unitCommanders.ToList();
            foreach (var enemyGroup in enemyGroups)
            {
                var selfGroup = DefenseService.GetDefenseGroup(enemyGroup, AvailableCommanders);
                if (selfGroup.Count() > 0)
                {
                    AvailableCommanders.RemoveAll(a => selfGroup.Any(s => a.UnitCalculation.Unit.Tag == s.UnitCalculation.Unit.Tag));
                }
                ArmySplits.Add(new ArmySplits { EnemyGroup = enemyGroup, SelfGroup = selfGroup });
            }
        }
    }
}
