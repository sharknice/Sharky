namespace Sharky.Builds.ProductionSelectors
{
    public class SimpleProducerSelector : IProducerSelector
    {
        public UnitCommander SelectBestProducer(UnitTypes unitType, IEnumerable<UnitCommander> producers)
        {
            return producers.First();
        }
    }
}
