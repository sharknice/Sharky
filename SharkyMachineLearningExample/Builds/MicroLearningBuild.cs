using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Attack;
using Sharky.MicroTasks;
using Sharky;
using SharkyMachineLearningExample.Tasks;

namespace SharkyMachineLearningExample.Builds
{
    public class MicroLearningBuild(DefaultSharkyBot defaultSharkyBot) : SharkyBuild(defaultSharkyBot)
    {
        protected LearningSubAttackTask LearningSubAttackTask;
        protected AdvancedAttackTask AdvancedAttackTask;
        protected MicroTargetingService MicroTargetingService = new MicroTargetingService(defaultSharkyBot);
        protected TargetingData TargetingData = defaultSharkyBot.TargetingData;

        int Round = 0;
        bool RoundActive = false;
        int Wins = 0;
        int Losses = 0;
        int Ties = 0;

        List<Unit> RoundStartUnits = new List<Unit>();
        List<Unit> RoundStartEnemyUnits = new List<Unit>();

        public override void StartBuild(int frame)
        {
            Round = 0;
            RoundActive = false;
            Wins = 0;
            Losses = 0;
            Ties = 0;

            base.StartBuild(frame);

            AttackData.UseAttackDataManager = false;
            AttackData.Attacking = true;

            AdvancedAttackTask = (AdvancedAttackTask)MicroTaskData[typeof(AttackTask).Name];
            AdvancedAttackTask.AllowSplit = false;

            LearningSubAttackTask = (LearningSubAttackTask)AdvancedAttackTask.SubTasks[nameof(LearningSubAttackTask)];
        }

        public override void OnFrame(ResponseObservation observation)
        {
            TargetingData.AttackPoint = MicroTargetingService.GetMicroAttackPoint(TargetingData.AttackPoint);
            TargetingData.ForwardDefensePoint = MicroTargetingService.GetMicroDefensePoint(TargetingData.AttackPoint, TargetingData.ForwardDefensePoint);

            if (RoundActive && DetectedRoundEnd())
            {
                RoundActive = false;
                if (Round == 10)
                {
                    PrintResult();
                }
                OnRoundEnd();
            }
            if (!RoundActive && DetectedRoundStart())
            {
                Round++;
                RoundActive = true;

                OnRoundStart();
            }
        }

        public virtual void OnRoundEnd()
        {
            LearningSubAttackTask.Disable();

            ActiveUnitData.EnemyUnits.Clear();
            ActiveUnitData.SelfUnits.Clear();
            ActiveUnitData.Commanders.Clear();
        }

        public virtual void OnRoundStart()
        {
            var keysToRemove = ActiveUnitData.Commanders.Where(kvp => kvp.Value.UnitCalculation.FrameLastSeen != MacroData.Frame).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                ActiveUnitData.Commanders.Remove(key);
            }

            keysToRemove = ActiveUnitData.SelfUnits.Where(kvp => kvp.Value.FrameLastSeen != MacroData.Frame).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                ActiveUnitData.SelfUnits.Remove(key);
            }

            keysToRemove = ActiveUnitData.EnemyUnits.Where(kvp => kvp.Value.FrameLastSeen != MacroData.Frame).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                ActiveUnitData.EnemyUnits.Remove(key);
            }

            LearningSubAttackTask.Enable();
            LearningSubAttackTask.ClaimUnitsFromParent(AdvancedAttackTask.UnitCommanders);

            RoundStartUnits = ActiveUnitData.Commanders.Values.Select(c => c.UnitCalculation.Unit).ToList();
            RoundStartEnemyUnits = ActiveUnitData.EnemyUnits.Values.Select(c => c.Unit).ToList();
        }

        private void PrintResult()
        {
            var result = "Tied";
            if (Wins > Losses)
            {
                result = "Won";
            }
            if (Losses > Wins)
            {
                result = "Lost";
            }
            Console.WriteLine($"{result}: {Wins}-{Losses}-{Ties}");
            TagService.Tag($"{result}: {Wins}-{Losses}-{Ties}");
        }

        private bool DetectedRoundStart()
        {
            if (ActiveUnitData.Commanders.Any() && ActiveUnitData.EnemyUnits.Any())
            {
                return true;
            }
            return false;
        }

        bool DetectedRoundEnd()
        {
            if (!ActiveUnitData.Commanders.Any() && !ActiveUnitData.EnemyUnits.Any())
            {
                Console.WriteLine($"Tied Round {Round}");
                TagService.Tag($"Tied Round {Round}");
                Ties++;
                LearningSubAttackTask.Tied();
                return true;
            }
            if (!ActiveUnitData.Commanders.Any())
            {
                Console.WriteLine($"Lost Round {Round}");
                TagService.Tag($"Lost Round {Round}");
                LearningSubAttackTask.Lost(RoundStartEnemyUnits, ActiveUnitData.EnemyUnits.Values.Select(c => c.Unit).ToList());
                Losses++;
                return true;
            }
            if (!ActiveUnitData.EnemyUnits.Any())
            {
                Console.WriteLine($"Won Round {Round}");
                TagService.Tag($"Won Round {Round}");
                LearningSubAttackTask.Won(RoundStartUnits, ActiveUnitData.Commanders.Values.Select(c => c.UnitCalculation.Unit).ToList());
                Wins++;
                return true;
            }

            return false;
        }
    }
}