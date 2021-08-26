namespace Sharky.MicroTasks.Attack
{
    public interface IAttackService
    {
        public IMicroTask AttackTask { get; set; }
        public bool Attack();
    }
}
