namespace Sharky.Builds.ProductionSelectors
{
    /// <summary>
    /// Selectors decide which producer is used for production.
    /// For example you can use it to decide which overlord is transmorfed to overseer if there are overseers requested to be produced
    /// </summary>
    public interface IProducerSelector
    {
        /// <summary>
        /// Select best producer unit for given unit type
        /// </summary>
        /// <param name="unitType">Unit type to produce</param>
        /// <param name="producers">Units that can produce that unit type</param>
        /// <returns></returns>
        UnitCommander SelectBestProducer(UnitTypes unitType, IEnumerable<UnitCommander> producers);
    }
}
