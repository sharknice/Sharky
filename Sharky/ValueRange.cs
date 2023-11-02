namespace Sharky.Helper
{
    public readonly struct ValueRange
    {
        private readonly int min;
        private readonly int max;
        private readonly string key;

        public readonly int Min { get => min; }

        public readonly int Max { get => max; }

        public readonly string Key { get => key; }

        public ValueRange(int min, int max)
        {
            key = Guid.NewGuid().ToString();

            this.min = min;
            this.max = max;
        }

        public static implicit operator ValueRange(int x) => new(x, x);

        public static implicit operator int(ValueRange v) => ValueCallbackService.GetValue(v);

        public static bool operator <(ValueRange v, int b) => v < b;

        public static bool operator >(ValueRange v, int b) => v > b;

        public static bool operator <=(ValueRange v, int b) => v <= b;

        public static bool operator >=(ValueRange v, int b) => v >= b;

        public static ValueRange operator ++(ValueRange v) => new(v.min + 1, v.max + 1);

        public static ValueRange operator --(ValueRange v) => new(v.min - 1, v.max - 1);
    }
}
