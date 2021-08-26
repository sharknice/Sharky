namespace Sharky
{
    public enum UnitRole
    {
        None,
        Bait,
        Scout,
        PreBuild,
        Build,
        Proxy,
        Minerals,
        Gas,
        Defend,
        Attack,
        PreventGasSteal,
        PreventBuildingLand,
        Wall,
        Door, // TODO: move to door position if not there, hold position if there
        Harass
    }
}
