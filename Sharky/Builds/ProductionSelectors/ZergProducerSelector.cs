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

            if (unitType == UnitTypes.ZERG_LURKERMP || unitType == UnitTypes.ZERG_RAVAGER || unitType == UnitTypes.ZERG_BANELING || unitType == UnitTypes.ZERG_BROODLORD)
            {
                return SelectBestCoconProducer(producers);
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

        private UnitCommander SelectBestCoconProducer(IEnumerable<UnitCommander> producers)
        {
            // Morph most damaged units that are least in danger as they recover all hp with the morph
            return producers.OrderBy(x => x.UnitCalculation.Unit.Health / x.UnitCalculation.Unit.HealthMax).OrderBy(x => x.UnitCalculation.EnemiesThreateningDamage.Count).First();
        }
    }
}
