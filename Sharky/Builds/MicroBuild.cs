namespace Sharky.Builds
{
    public class MicroBuild : SharkyBuild
    {
        protected AdvancedAttackTask AdvancedAttackTask;
        protected MicroTargetingService MicroTargetingService;
        protected TargetingData TargetingData;

        int Round = 0;
        bool RoundActive = false;
        int Wins = 0;
        int Losses = 0;
        int Ties = 0;

        public MicroBuild(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            MicroTargetingService = new MicroTargetingService(defaultSharkyBot);
            TargetingData = defaultSharkyBot.TargetingData;
        }

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
                TagService.Tag($"Tied Round {Round}");
                Ties++;
                return true;
            }
            if (!ActiveUnitData.Commanders.Any())
            {
                TagService.Tag($"Lost Round {Round}");
                Losses++;
                return true;
            }
            if (!ActiveUnitData.EnemyUnits.Any())
            {
                TagService.Tag($"Won Round {Round}");
                Wins++;
                return true;
            }

            return false;
        }
    }
}
