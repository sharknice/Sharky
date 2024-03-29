﻿namespace Sharky.MicroTasks.Zerg
{
    public class BurrowDronesFromHarras : MicroTask
    {
        EnemyData EnemyData;
        BuildOptions BuildOptions;
        SharkyUnitData UnitData;
        MapData MapData;
        MicroTaskData MicroTaskData;
        MacroData MacroData;
        ChatService ChatService;
        TagService TagService;

        private bool tagged = false;

        public BurrowDronesFromHarras(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitData = defaultSharkyBot.SharkyUnitData;
            MapData = defaultSharkyBot.MapData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            MacroData = defaultSharkyBot.MacroData;
            ChatService = defaultSharkyBot.ChatService;
            TagService = defaultSharkyBot.TagService;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
            {
                Disable();
                return;
            }

            if (!UnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW) || !BuildOptions.ZergBuildOptions.BurrowDronesFromHarrass)
            {
                return;
            }

            foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_DRONEBURROWED 
                || (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE && (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.Gas || commander.Value.UnitRole == UnitRole.Hide)))))
            {
                if (!UnitCommanders.Contains(commander.Value)
                    && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_DRONEBURROWED || BurrowWanted(commander.Value)))
                {
                    if (!tagged)
                    {
                        TagService.Tag("self_burrow_drones");
                        ChatService.SendChatType("burrow_drones");
                        tagged = true;
                    }

                    MicroTaskData.StealCommanderFromAllTasks(commander.Value);

                    UnitCommanders.Add(commander.Value);
                    commander.Value.UnitRole = UnitRole.Hide;
                    commander.Value.Claimed = true;
                }
            }
        }

        private bool BurrowWanted(UnitCommander unitCommander)
        {
            // TODO: better decision - when to burrow - maybe we should not burrow when few zerglings are harassing, but we want to burrow from splash/air units even when army is less than X
            return unitCommander.UnitRole != UnitRole.Attack && unitCommander.UnitRole != UnitRole.Defend && unitCommander.UnitRole != UnitRole.PreventBuildingLand && MacroData.FoodArmy >= 6 && IsInDanger(unitCommander.UnitCalculation) && !IsDetected(unitCommander.UnitCalculation);
        }

        /// <summary>
        /// I am in danger *chuckles*
        /// </summary>
        /// <returns></returns>
        private bool IsInDanger(UnitCalculation unitCalculation)
        {
            return unitCalculation.EnemiesInRangeOfAvoid.Count > 0;
        }

        private bool IsDetected(UnitCalculation unitCalculation)
        {
            return MapData.Map[(int)unitCalculation.Position.X,(int)unitCalculation.Position.Y].InEnemyDetection;
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var actions = new List<SC2Action>();

            foreach (var drone in UnitCommanders)
            {
                bool burrowWanted = BurrowWanted(drone);

                if (burrowWanted && !drone.UnitCalculation.Unit.IsBurrowed)
                {
                    actions.AddRange(drone.Order(frame, Abilities.BURROWDOWN_DRONE));
                    drone.UnitRole = UnitRole.Hide;
                }
                else if (!burrowWanted)
                {
                    if (drone.UnitCalculation.Unit.IsBurrowed)
                    {
                        actions.AddRange(drone.Order(frame, Abilities.BURROWUP_DRONE));
                    }

                    drone.UnitRole = UnitRole.None;
                    drone.Claimed = false;
                }
            }

            UnitCommanders.RemoveAll(x => x.UnitRole != UnitRole.Hide);

            return actions;
        }
    }
}
