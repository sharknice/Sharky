namespace Sharky.MicroTasks.Zerg
{
    public class BurrowBlockExpansionsTask : MicroTask
    {
        EnemyData EnemyData;
        IndividualMicroController IndividualMicroController;
        BuildOptions BuildOptions;
        SharkyUnitData UnitData;
        BaseData BaseData;
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;

        public BurrowBlockExpansionsTask(DefaultSharkyBot defaultSharkyBot, float priority, IndividualMicroController individualMicroController, SharkyUnitData unitData, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitData = unitData;
            BaseData = defaultSharkyBot.BaseData;
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            IndividualMicroController = individualMicroController;
            TargetingData = defaultSharkyBot.TargetingData;

            Priority = priority;
            Enabled = enabled;

            CommanderDebugText = "Blocking expansion";
            CommanderDebugColor = new Color() { R = 255, G = 63, B = 32 };
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
            {
                Disable();
                return;
            }

            if (!UnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW) || EnemyData.EnemyAggressivityData.ArmyAggressivity > 0.5f || AreLingsNeededElsewhere())
            {
                return;
            }

            foreach (var commander in commanders.Where(commander => ((commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED) && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Attack))))
            {
                if (UnitCommanders.Count >= BuildOptions.ZergBuildOptions.MaxBurrowedBlockingZerglings)
                    break;

                commander.Value.Claimed = true;
                commander.Value.UnitRole = UnitRole.BlockExpansion;
                UnitCommanders.Add(commander.Value);
            }
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var actions = new List<SC2Action>();

            if (AreLingsNeededElsewhere())
            {
                StopBlocking(frame, actions);
                return actions;
            }

            if (!UnitCommanders.Any())
                return actions;

            var points = GetBlockingPoints();

            foreach (var ling in UnitCommanders)
            {
                var point = points.FirstOrDefault();
                points = points.Skip(1);

                if (!FreeOurExpansions(ling, frame, point, actions) && point != null)
                {
                    if (point != null)
                    {
                        actions.AddRange(BlockExpansion(ling, point, frame));
                    }
                }
            }

            return actions;
        }

        private void StopBlocking(int frame, List<SC2Action> actions)
        {
            foreach (var commander in UnitCommanders)
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
                actions.AddRange(commander.Order(frame, Abilities.BURROWUP_ZERGLING));
            }

            UnitCommanders.Clear();
        }

        // In some emergency situations we stop blocking and use the lings
        private bool AreLingsNeededElsewhere()
        {
            return MacroData.FoodUsed < 40;
        }

        private bool FreeOurExpansions(UnitCommander ling, int frame, Point2D pos, List<SC2Action> actions)
        {
            if ((ling.UnitCalculation.Unit.IsBurrowed || (pos != null && pos.ToVector2().Distance(ling.UnitCalculation.Position) < 6))
                && ling.UnitCalculation.NearbyAllies.Any(x => x.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE && x.Position.Distance(ling.UnitCalculation.Position) < 8))
            {
                actions.AddRange(ling.Order(frame, Abilities.BURROWUP_ZERGLING));
                return true;
            }

            return false;
        }

        private IEnumerable<SC2Action> BlockExpansion(UnitCommander ling, Point2D pos, int frame)
        {
            if (pos == null)
                return new List<SC2Action>();

            var distanceFromTarget = pos.Distance(ling.UnitCalculation.Unit.Pos.ToPoint2D());

            if (distanceFromTarget > 1.5f)
            {
                if (ling.UnitCalculation.Unit.IsBurrowed && distanceFromTarget > 10 && (ling.UnitCalculation.NearbyAllies.Any(z => z.Unit.UnitType == (int)UnitTypes.ZERG_ZERGLINGBURROWED && z.Position.Distance(ling.UnitCalculation.Position) < 5) || distanceFromTarget > 36))
                {
                    return ling.Order(frame, Abilities.BURROWUP_ZERGLING);
                }

                return IndividualMicroController.NavigateToPoint(ling, pos, pos, pos, frame);
            }
            else
            {
                return ling.Order(frame, Abilities.BURROWDOWN_ZERGLING);
            }
        }

        private IEnumerable<Point2D> GetBlockingPoints()
        {
            var points = BaseData.BaseLocations
                .Where(b => BaseMakesSenseToTake(b))
                .Where(b => !BaseData.EnemyBases.Contains(b))
                .OrderBy(b => EnemyData.EnemyAggressivityData.DistanceGrid.GetDist(b.Location.X, b.Location.Y, false))
                .Take(BuildOptions.ZergBuildOptions.MaxBurrowedBlockingZerglings);

            return points.Select(x => x.Location);
        }

        /// <summary>
        /// Returns true if the base still seems to have some resources to mine
        /// </summary>
        private bool BaseMakesSenseToTake(BaseLocation baseLoc)
        {
            // Returns true if there is enough resources in the base to make at least some sense to block it
            return baseLoc.MineralFields.Count>4 || baseLoc.MineralFields.Where(x => x.HasMineralContents).Sum(x => x.MineralContents) > 400 || baseLoc.VespeneGeysers.Where(x => x.HasVespeneContents).Sum(x => x.VespeneContents) > 300;
        }
    }
}
