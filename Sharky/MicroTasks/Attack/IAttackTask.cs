namespace Sharky.MicroTasks.Attack
{
    public interface IAttackTask : IMicroTask
    {
        void GiveCommanderToChild(UnitCommander commander);
        IEnumerable<UnitCommander> GetAvailableCommanders();
    }
}
