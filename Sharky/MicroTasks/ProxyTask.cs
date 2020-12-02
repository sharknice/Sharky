using Sharky.Managers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ProxyTask : MicroTask
    {
        UnitDataManager UnitDataManager;
        MacroData MacroData;
        MicroManager MicroManager;
        public string ProxyName { get; set; }

        bool Enabled { get; set; }

        bool started { get; set; }

        public ProxyTask(UnitDataManager unitDataManager, bool enabled, float priority, MacroData macroData, string proxyName, MicroManager microManager)
        {
            UnitDataManager = unitDataManager;
            Priority = priority;
            MacroData = macroData;
            ProxyName = proxyName;
            MicroManager = microManager;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public void Enable()
        {
            if (MicroManager.MicroTasks.ContainsKey("MiningTask"))
            {
                MicroManager.MicroTasks["MiningTask"].ResetClaimedUnits();
            }
            Enabled = true;
        }

        public void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (Enabled)
            {
                if (UnitCommanders.Count() == 0)
                {
                    if (started)
                    {
                        Disable();
                        return;
                    }

                    foreach (var commander in commanders)
                    {
                        if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                        {
                            if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !UnitDataManager.MiningAbilities.Contains((Abilities)o.AbilityId)))
                            {
                            }
                            else
                            {
                                commander.Value.Claimed = true;
                                UnitCommanders.Add(commander.Value);
                                started = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (MacroData.Proxies.ContainsKey(ProxyName))
            {
                commands.AddRange(MoveToProxyLocation(frame));
            }

            return commands;
        }

        IEnumerable<SC2APIProtocol.Action> MoveToProxyLocation(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => UnitDataManager.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
            {
                if (Vector2.DistanceSquared(new Vector2(MacroData.Proxies[ProxyName].Location.X, MacroData.Proxies[ProxyName].Location.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) > MacroData.Proxies[ProxyName].MaximumBuildingDistance)
                {
                    var action = commander.Order(frame, Abilities.MOVE, MacroData.Proxies[ProxyName].Location);
                    if (action != null)
                    {
                        commands.Add(action);
                    }
                }
            }

            return commands;
        }
    }
}
