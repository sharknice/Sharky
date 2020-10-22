using SC2APIProtocol;

namespace Sharky.MapAnalysis
{
    public class ArrayBoolGrid : BoolGrid
    {
        private bool[,] data;
        public ArrayBoolGrid(int width, int height)
        {
            data = new bool[width, height];
        }

        public override BoolGrid Clone()
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y];
            return result;
        }

        internal override bool GetInternal(Point2D pos)
        {
            return data[(int)pos.X, (int)pos.Y];
        }

        internal void Set(Point2D pos, bool val)
        {
            data[(int)pos.X, (int)pos.Y] = val;
        }

        public new bool this[Point2D pos]
        {
            get { return Get(pos); }
            set { data[(int)pos.X, (int)pos.Y] = value; }
        }

        public new bool this[int x, int y]
        {
            get { return Get(SC2Util.Point(x, y)); }
            set { data[x, y] = value; }
        }

        public override int Width()
        {
            return data.GetLength(0);
        }

        public override int Height()
        {
            return data.GetLength(1);
        }
    }
}
