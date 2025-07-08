namespace Sharky.Managers
{
    public class EnemyStrategyManager : SharkyManager
    {
        EnemyData EnemyData;
        EnemyAggressivityService EnemyAggressivityService;
        DebugService DebugService;

        public bool ShowDebugText { get; set; } = true;

        public EnemyStrategyManager(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            EnemyAggressivityService = defaultSharkyBot.EnemyAggressivityService;
            DebugService = defaultSharkyBot.DebugService;
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            foreach (var enemyStrategy in EnemyData.EnemyStrategies.Values)
            {
                enemyStrategy.OnFrame(frame);
            }

            EnemyAggressivityService.Update(frame);

            if (ShowDebugText)
            {
                DebugService.DrawText($"Enemy aggression {EnemyData.EnemyAggressivityData.ArmyAggressivity}");
            }

            return new List<SC2Action>();
        }
    }
}
