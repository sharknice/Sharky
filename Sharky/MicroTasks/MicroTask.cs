namespace Sharky.MicroTasks
{
    public abstract class MicroTask : IMicroTask
    {
        public int Deaths { get; protected set; }

        public List<UnitCommander> UnitCommanders { get; set; } = new List<UnitCommander>();
        public float Priority { get; set; }

        public bool Enabled { get; protected set; }
        public double LongestFrame { get; set; }
        public double TotalFrameTime { get; set; }

        public virtual void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            throw new NotImplementedException();
        }

        public virtual void ResetClaimedUnits()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
                commander.UnitRole = UnitRole.None;
            }
            UnitCommanders = new List<UnitCommander>();
        }

        public virtual List<UnitCommander> ResetNonEssentialClaims()
        {
            var unlcaimed = new List<UnitCommander>();
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
                commander.UnitRole = UnitRole.None;
                unlcaimed.Add(commander);
            }
            UnitCommanders = new List<UnitCommander>();
            return unlcaimed;
        }

        public virtual void Enable()
        {
            if (Enabled) { return; }
            Enabled = true;
            Console.WriteLine($"Enable {GetType().Name}");
        }

        public virtual void Disable()
        {
            if (!Enabled) { return; }

            Console.WriteLine($"Disable {GetType().Name}");
            PrintReport(1);
            ResetClaimedUnits();
            Enabled = false;         
        }

        public virtual void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                Deaths += UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }

        /// <summary>
        /// Steals unit completely from this microtask commanders
        /// </summary>
        /// <param name="commander"></param>
        public virtual void StealUnit(UnitCommander commander)
        {
            UnitCommanders.Remove(commander);
        }

        public override string ToString()
        {
            return !Enabled ? "<Disabled>" : $"Commanders: ({UnitCommanders.Count}) {string.Join(", ", UnitCommanders.Select(x => x.ToString()))}";
        }

        public virtual void PrintReport(int frame)
        {
            Console.WriteLine($"     Deaths: {Deaths}, Frames - Longest: {LongestFrame:F2}ms, Average: {TotalFrameTime/frame:F2}ms, Total: {TotalFrameTime:F2}ms");
        }

        public string? CommanderDebugText { get; set; }
        public Color? CommanderDebugColor { get; set; }

        public virtual void DebugUnits(DebugService debugService)
        {
            foreach (var unit in UnitCommanders)
            {
                debugService.DebugUnitText(unit.UnitCalculation, $"{CommanderDebugText ?? GetType().Name.Replace("Task", "", StringComparison.InvariantCultureIgnoreCase)}, {unit.UnitRole}", CommanderDebugColor ?? debugService.DefaultMicroTaskColor);
            }
        }
    }
}
