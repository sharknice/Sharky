using Sharky.Helper;

namespace Sharky
{
    public static class ValueCallbackService
    {
        private static event Func<ValueRange, int> callback;

        public static void Init(Func<ValueRange, int> cb)
        {
            callback = cb;
        }

        public static int GetValue(ValueRange range)
        {
            if (range.Min == range.Max)
                return range.Min;

            if (callback == null)
                throw new ArgumentNullException($"Static class {nameof(ValueCallbackService)} must be initialized with a value-function using the method {nameof(ValueCallbackService)}.{nameof(Init)}!");
            
            return callback(range);
        }
    }
}
