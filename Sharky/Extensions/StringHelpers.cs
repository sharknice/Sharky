namespace Sharky.Extensions
{
    public static class StringHelpers
    {
        public static string ToString(UnitOrder order)
        {
            if (order == null)
                return "No orders";

            StringBuilder sb = new(100);
            sb.Append((Abilities)order.AbilityId);
            if (order.TargetWorldSpacePos != null)
            {
                sb.Append(" to pos: ");
                sb.Append(order.TargetWorldSpacePos);
            }

            if (order.TargetUnitTag != 0)
            {
                sb.Append(" target: ");
                sb.Append(order.TargetUnitTag);
            }
            return sb.ToString();
        }
    }
}
