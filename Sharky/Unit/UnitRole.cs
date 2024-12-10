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
        Door,
        Harass,
        Support,
        Repair,
        SpawnLarva,
        
        /// <summary>
        /// Idle queen assigned to creep spread
        /// </summary>
        SpreadCreepWait,

        /// <summary>
        /// Queen assigned to spread creep, walking towards next creep point
        /// </summary>
        SpreadCreepWalk,

        /// <summary>
        /// Queen assigned to creep spread, ordered to create tumor and in cast distance to the tumor position
        /// </summary>
        SpreadCreepCast,

        Morph,
        Die,
        ChaseReaper,
        WallOff,
        Regenerate,
        Hide,
        Regroup,
        BlockExpansion,
        Leader,
        NextLeader,
        Disband,
        RunAway,
        Chase,
        SaveEnergy,
        Cancel,
        Save,
        DoNotDefend
    }
}
