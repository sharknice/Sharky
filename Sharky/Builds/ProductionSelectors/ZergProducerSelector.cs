namespace Sharky.Builds.ProductionSelectors
{
    public class ZergProducerSelector : IProducerSelector
    {
        private EnemyData EnemyData;
        private ActiveUnitData ActiveUnitData;
        private SharkyUnitData SharkyUnitData;
        private BaseData BaseData;
        private TargetingData TargetingData;

        public ZergProducerSelector(DefaultSharkyBot sharkyBot)
        {
            EnemyData = sharkyBot.EnemyData;
            ActiveUnitData = sharkyBot.ActiveUnitData;
            SharkyUnitData = sharkyBot.SharkyUnitData;
            BaseData = sharkyBot.BaseData;
            TargetingData = sharkyBot.TargetingData;
        }

        public UnitCommander SelectBestProducer(UnitTypes unitType, IEnumerable<UnitCommander> producers)
        {
            if (unitType == UnitTypes.ZERG_OVERSEER)
            {
                return SelectBestOverseerProducer(producers);
            }

            return producers.First();
        }

        private UnitCommander SelectBestOverseerProducer(IEnumerable<UnitCommander> producers)
        {
            Vector2 desiredMorphPoint;
            if (EnemyData.EnemyStrategies[nameof(InvisibleAttacks)].Detected || EnemyData.EnemyStrategies[nameof(InvisibleAttacksSuspected)].Active)
            {
                var enemyInvisibleUnits = ActiveUnitData.EnemyUnits.Where(x => InvisibleAttacks.IsNonObserverCloakableUnit(x.Value)).Select(x => x.Value);

                if (enemyInvisibleUnits.Any())
                {
                    // Morph near invisible units
                    desiredMorphPoint = enemyInvisibleUnits.First().Position;
                    // todo: order by whether unit is detected to prefer morphing near non-detected units
                }
                else
                {
                    // Morph at home to be as safe as possible against invis
                    desiredMorphPoint = TargetingData.NaturalBasePoint.ToVector2();
                }
            }
            else
            {
                // Morph near enemy base for scouting
                desiredMorphPoint = TargetingData.NaturalFrontScoutPoint.ToVector2();
            }

            return producers.OrderBy(x => x.UnitCalculation.Position.DistanceSquared(desiredMorphPoint)).OrderBy(x => x.UnitCalculation.EnemiesThreateningDamage.Count).First();
        }
    }
}
