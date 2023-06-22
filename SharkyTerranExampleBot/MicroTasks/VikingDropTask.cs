using Sharky;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using System.Collections.Generic;
using System.Linq;

namespace SharkyTerranExampleBot.MicroTasks
{
    public class VikingDropTask : MicroTask
    {
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;

        public VikingDropTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            BaseData = defaultSharkyBot.BaseData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            TargetingData = defaultSharkyBot.TargetingData;

            UnitCommanders = new List<UnitCommander>();
            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(c => !c.Value.Claimed && (c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC)))
            {
                commander.Value.Claimed = true;
                UnitCommanders.Add(commander.Value);
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var medivacs = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC);
            var assaulters = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGASSAULT);
            var fighters = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER);

            actions.AddRange(MorphFighters(fighters, frame));
            actions.AddRange(MedivacActions(medivacs, frame));
            actions.AddRange(AssaultActions(assaulters, frame));

            return actions;
        }

        IEnumerable<SC2APIProtocol.Action> MorphFighters(IEnumerable<UnitCommander> fighters, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in fighters)
            {
                var action = commander.Order(frame, Abilities.MORPH_VIKINGASSAULTMODE);
                if (action != null)
                {
                    actions.AddRange(action);
                }
            }

            return actions;
        }

        private IEnumerable<SC2APIProtocol.Action> MedivacActions(IEnumerable<UnitCommander> medivacs, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in medivacs)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_MEDIVAC))
                {
                    continue;
                }
                else
                {
                    if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT))
                    {
                        if (commander.UnitCalculation.Unit.CargoSpaceTaken < commander.UnitCalculation.Unit.CargoSpaceMax)
                        {
                            var nearestAssault = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGASSAULT && !a.Loaded);
                            if (nearestAssault != null)
                            {
                                var action = commander.Order(frame, Abilities.LOAD, targetTag: nearestAssault.Unit.Tag);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                                continue;
                            }
                        }
                        else
                        {
                            var dropBase = BaseData.EnemyBaseLocations.FirstOrDefault();
                            if (dropBase != null)
                            {
                                var action = commander.Order(frame, Abilities.UNLOADALLAT_MEDIVAC, dropBase.BehindMineralLineLocation);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                                continue;
                            }
                        }
                    }
                    else
                    {
                        var starport = ActiveUnitData.SelfUnits.FirstOrDefault(a => a.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT).Value;
                        if (starport != null)
                        {
                            var action = commander.Order(frame, Abilities.MOVE, targetTag: starport.Unit.Tag);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                            continue;
                        }
                    }
                }
            }

            return actions;
        }

        private IEnumerable<SC2APIProtocol.Action> AssaultActions(IEnumerable<UnitCommander> assaulters, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in assaulters)
            {
                if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT))
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetingData.AttackPoint);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                    continue;
                }
            }

            return actions;
        }
    }
}
