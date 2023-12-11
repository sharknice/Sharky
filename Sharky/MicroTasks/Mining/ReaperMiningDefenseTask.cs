namespace Sharky.MicroTasks.Mining
{
    public class ReaperMiningDefenseTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        MineralWalker MineralWalker;
        MapDataService MapDataService;

        UnitCalculation EnemyReaper;

        public ReaperMiningDefenseTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            MineralWalker = defaultSharkyBot.MineralWalker;
            MapDataService = defaultSharkyBot.MapDataService;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg || EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                Disable();
            }

            var commands = new List<SC2APIProtocol.Action>();

            GetEnemyReaper();
          
            commands = DefendAgainstReaper(frame);

            if (EnemyReaper == null)
            {
                UnitCommanders.ForEach(c => c.UnitRole = UnitRole.None);
            }

            UnitCommanders.RemoveAll(c => c.UnitRole != UnitRole.ChaseReaper && c.UnitRole != UnitRole.RunAway);

            return commands;
        }

        private List<SC2APIProtocol.Action> DefendAgainstReaper(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyReaper != null && UnitCommanders.Count() < 3)
            {
                ClaimDefenders();
            }

            foreach (var commander in UnitCommanders)
            {
                var healthRequired = 15;
                if (UnitCommanders.Count(c => c.UnitRole == UnitRole.ChaseReaper) < 3)
                {
                    healthRequired = 33;
                }
                if (EnemyReaper == null || commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield <= healthRequired || !commander.UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
                {
                    commander.UnitRole = UnitRole.RunAway;
                    List<SC2APIProtocol.Action> action;
                    if (MineralWalker.MineralWalkHome(commander, frame, out action))
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, targetTag: EnemyReaper.Unit.Tag);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }

        private void ClaimDefenders()
        {
            var worker = EnemyReaper.NearbyEnemies.FirstOrDefault(e => e.UnitClassifications.Contains(UnitClassification.Worker) && 
                e.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter) &&
                ActiveUnitData.Commanders.ContainsKey(e.Unit.Tag) && ActiveUnitData.Commanders[e.Unit.Tag].UnitRole != UnitRole.ChaseReaper && 
                e.Unit.Health + e.Unit.Shield >= 40));

            if (worker != null)
            {
                ActiveUnitData.Commanders[worker.Unit.Tag].UnitRole = UnitRole.ChaseReaper;
                UnitCommanders.Add(ActiveUnitData.Commanders[worker.Unit.Tag]);
            }
        }

        private void GetEnemyReaper()
        {
            EnemyReaper = ActiveUnitData.EnemyUnits.Values.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER
                            && e.NearbyEnemies.Any(ee => ee.UnitClassifications.Contains(UnitClassification.ResourceCenter) && MapDataService.MapHeight(ee.Unit.Pos) == MapDataService.MapHeight(e.Unit.Pos) && !ee.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN))
                            && !e.NearbyAllies.Any(ee => ee.UnitClassifications.Contains(UnitClassification.ArmyUnit) || ee.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN)
                            && !e.NearbyEnemies.Any(ee => ee.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !ee.Unit.IsFlying));
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                if (EnemyReaper != null && EnemyReaper.Unit.Tag == tag)
                {
                    EnemyReaper = null;
                }
            }
            base.RemoveDeadUnits(deadUnits);
        }
    }
}
