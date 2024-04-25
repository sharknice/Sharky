﻿namespace Sharky.MicroTasks.Zerg
{
    public class QueenInjectTask : MicroTask
    {
        EnemyData EnemyData;
        UnitCountService UnitCountService;
        BuildOptions BuildOptions;
        QueenMicroController QueenMicroController;

        List<InjectData> HatcheryQueenPairing = new List<InjectData>();

        public QueenInjectTask(DefaultSharkyBot defaultSharkyBot, float priority, QueenMicroController queenMicroController, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            BuildOptions = defaultSharkyBot.BuildOptions;
            QueenMicroController = queenMicroController;

            Priority = priority;
            Enabled = enabled;

            CommanderDebugColor = new SC2APIProtocol.Color() { R = 127, G = 255, B = 32 };
            CommanderDebugText = "Injecting";
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return;
            }

            if (BuildOptions.ZergBuildOptions.SecondQueenPreferCreepFirst && UnitCommanders.Count == 1 && UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_CREEPTUMORBURROWED) < 1)
            {
                return;
            }

            UpdateHatcheriesList(commanders);

            // Remove queens with other unit roles
            foreach (var c in UnitCommanders)
            {
                if (c.UnitRole != UnitRole.SpawnLarva)
                {
                    var pair = HatcheryQueenPairing.FirstOrDefault(x => x.Queen == c);
                    if (pair != null)
                    {
                        pair.Queen = null;
                    }
                }
            }
            UnitCommanders.RemoveAll(q => q.UnitRole != UnitRole.SpawnLarva);

            var availableQueens = commanders.Values.Where(commander => (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED)
                    && (!commander.Claimed || commander.UnitRole == UnitRole.SpreadCreepWait));

            // TODO: find nearest free hatchery for queen - now queen from natural goes to main hatchery if born before queen in main

            foreach (var entry in HatcheryQueenPairing)
            {
                if (entry.Queen == null)
                {
                    var closest = availableQueens
                        .Where(q => !HatcheryQueenPairing.Any(hp => hp.Queen == q))
                        .OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, entry.Hatchery.UnitCalculation.Position)).FirstOrDefault();

                    if (closest != null)
                    {
                        closest.Claimed = true;
                        closest.UnitRole = UnitRole.SpawnLarva;
                        UnitCommanders.Add(closest);
                        entry.Queen = closest;
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var entry in HatcheryQueenPairing)
            {
                if (entry.Queen == null) continue;

                if (!WalkToHatch(frame, actions, entry.Queen, entry.Hatchery.UnitCalculation))
                    InjectLarva(frame, actions, entry.Queen, entry.Hatchery.UnitCalculation);
            }

            return actions;
        }

        bool WalkToHatch(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, UnitCalculation hatchery)
        {
            if ((frame - queen.LastOrderFrame > 5) && queen.UnitCalculation.Position.Distance(hatchery.Position) >= 4)
            {
                actions.AddRange(QueenMicroController.NavigateToPoint(queen, hatchery.Position.ToPoint2D(), hatchery.Position.ToPoint2D(), null, frame));
                return true;
            }

            return false;
        }

        bool InjectLarva(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, UnitCalculation hatchery)
        {
            if (queen.UnitCalculation.Unit.Energy >= 25 && (frame - queen.LastOrderFrame > 5) && hatchery.Unit.BuildProgress >= 0.99f && !hatchery.Unit.BuffIds.Contains((uint)Buffs.QUEENSPAWNLARVATIMER))
            {
                actions.AddRange(queen.Order(frame, Abilities.EFFECT_INJECTLARVA, targetTag: hatchery.Unit.Tag));
                return true;
            }

            return false;
        }

        private void UpdateHatcheriesList(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var hatchery in commanders.Values.Where(u => u.UnitCalculation.Unit.BuildProgress > 0.9 && u.UnitCalculation.UnitClassifications.HasFlag(UnitClassification.ResourceCenter)))
            {
                var existing = HatcheryQueenPairing.FirstOrDefault(d => d.Hatchery.UnitCalculation.Unit.Tag == hatchery.UnitCalculation.Unit.Tag);
                if (existing == null)
                {
                    HatcheryQueenPairing.Add(new InjectData { Hatchery = hatchery });
                }
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            base.RemoveDeadUnits(deadUnits);

            foreach (var entry in HatcheryQueenPairing)
            {
                if (deadUnits.Contains(entry.Hatchery.UnitCalculation.Unit.Tag))
                {
                    entry.Hatchery = null;
                    if (entry.Queen != null)
                    {
                        entry.Queen.UnitRole = UnitRole.None;
                        entry.Queen.Claimed = false;
                    }
                }

                if (entry.Queen != null && deadUnits.Contains(entry.Queen.UnitCalculation.Unit.Tag))
                {
                    entry.Queen = null;
                }
            }

            HatcheryQueenPairing.RemoveAll(x => x.Hatchery == null);
        }

        public override void StealUnit(UnitCommander commander)
        {
            HatcheryQueenPairing.RemoveAll(x=>x.Queen == commander);

            base.StealUnit(commander);
        }
    }
}
