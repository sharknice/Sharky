namespace Sharky.Algorithm;

public enum SplitType
{
    BranchX,
    BranchY,
    BranchZ,
    Leaf,
}

public sealed class Tree2Node
{
    public int left;
    public int right;
    public float midValue;
    public SplitType splitType;
    public Tree2Node childL;
    public Tree2Node childR;
    public int childCount;
}

public struct Tree2NodeValue<T>
{
    public Vector2 position;
    public T value;
}

public sealed class KDTree2<T>
{
    struct BB
    {
        public Vector2 min;
        public Vector2 max;

        public bool Contains(Vector2 position)
        {
            return Vector2.Clamp(position, min, max) == position;
        }
    }

    public Tree2Node root;
    public List<Tree2NodeValue<T>> values = new List<Tree2NodeValue<T>>();

    public void Add(T value, Vector2 position)
    {
        values.Add(new Tree2NodeValue<T>()
        {
            position = position,
            value = value
        });
    }

    public void Clear()
    {
        root = null;
        values.Clear();
    }

    public void Build()
    {
        if (root != null)
            throw new Exception("build a tree twice");
        if (values.Count == 0)
        {
            root = new Tree2Node() { splitType = SplitType.Leaf };
            return;
        }

        Vector2 min = values[0].position;
        Vector2 max = values[0].position;
        for (int i = 1; i < values.Count; i++)
        {
            Tree2NodeValue<T> v = values[i];
            min = Vector2.Min(min, v.position);
            max = Vector2.Max(max, v.position);
        }
        root = _Build(0, values.Count, 0, new BB() { min = min, max = max });
    }

    public void ForRange(Vector2 position, float radius, Action<T> action)
    {
        _Search(root, position, radius, radius * radius, action);
    }

    public void ForRange(Vector2 min, Vector2 max, Action<T> action)
    {
        _Search(root, new BB() { min = Vector2.Min(min, max), max = Vector2.Max(min, max), }, action);
    }

    public void Nearest(Vector2 position, float maxDistance, Predicate<T> filter, Action<T> action)
    {
        int resultIndex = -1;
        float md = maxDistance;
        float md2 = md * md;
        _Nearest(root, position, filter, ref resultIndex, ref md, ref md2);
        if (resultIndex >= 0)
        {
            action(values[resultIndex].value);
        }
    }

    void _Search(Tree2Node node, Vector2 position, float radius, float r2, Action<T> action)
    {
        if (node.childCount == 0)
        {
            return;
        }
        if (node.splitType == SplitType.Leaf)
        {
            for (int i = node.left; i < node.right; i++)
            {
                var v = values[i];
                if (Vector2.DistanceSquared(position, v.position) <= r2)
                {
                    action(v.value);
                }
            }
        }
        else
        {
            float l;
            float r;
            if (node.splitType == SplitType.BranchX)
            {
                l = position.X - radius;
                r = position.X + radius;
            }
            else
            {
                l = position.Y - radius;
                r = position.Y + radius;
            }

            if (l <= node.midValue)
            {
                _Search(node.childL, position, radius, r2, action);
            }
            if (r >= node.midValue)
            {
                _Search(node.childR, position, radius, r2, action);
            }
        }
    }

    void _Search(Tree2Node node, BB bb, Action<T> action)
    {
        if (node.childCount == 0)
        {
            return;
        }
        if (node.splitType == SplitType.Leaf)
        {
            for (int i = node.left; i < node.right; i++)
            {
                var v = values[i];
                if (bb.Contains(v.position))
                {
                    action(v.value);
                }
            }
        }
        else
        {
            float l;
            float r;
            if (node.splitType == SplitType.BranchX)
            {
                l = bb.min.X;
                r = bb.max.X;
            }
            else
            {
                l = bb.min.Y;
                r = bb.max.Y;
            }

            if (l <= node.midValue)
            {
                _Search(node.childL, bb, action);
            }
            if (r >= node.midValue)
            {
                _Search(node.childR, bb, action);
            }
        }
    }

    void _Nearest(Tree2Node node, Vector2 position, Predicate<T> filter, ref int result, ref float md, ref float md2)
    {
        if (node.splitType == SplitType.Leaf)
        {
            for (int i = node.left; i < node.right; i++)
            {
                var v = values[i];
                var xd2 = Vector2.DistanceSquared(position, v.position);
                if (xd2 <= md2 && filter(v.value))
                {
                    result = i;
                    md2 = xd2;
                    if (xd2 > 0)
                        md = MathF.Sqrt(xd2);
                    else
                        md = 0;
                }
            }
        }
        else
        {
            float value;
            if (node.splitType == SplitType.BranchX)
            {
                value = position.X;
            }
            else
            {
                value = position.Y;
            }
            if (value - md <= node.midValue)
            {
                _Nearest(node.childL, position, filter, ref result, ref md, ref md2);
            }
            if (value + md >= node.midValue)
            {
                _Nearest(node.childR, position, filter, ref result, ref md, ref md2);
            }
        }
    }

    Tree2Node _Build(int left, int right, int deep, in BB bb)
    {
        if (right - left < 12 || deep > 8)
        {
            var child = new Tree2Node()
            {
                left = left,
                right = right,
                childCount = right - left,
                splitType = SplitType.Leaf,
            };
            return child;
        }

        Vector2 min = bb.min;
        Vector2 max = bb.max;

        Vector2 size = max - min;

        if (size.X > size.Y)
        {
            float midValue = (max.X + min.X) * 0.5f;
            int m = PartitionX(left, right, midValue);
            return _AddChild(left, right, m, midValue, deep, SplitType.BranchX, bb);
        }
        else
        {
            float midValue = (max.Y + min.Y) * 0.5f;
            int m = PartitionY(left, right, midValue);
            return _AddChild(left, right, m, midValue, deep, SplitType.BranchY, bb);
        }
    }

    Tree2Node _AddChild(int left, int right, int m, float midValue, int deep, SplitType splitType, in BB bb)
    {
        BB bb0 = bb;
        BB bb1 = bb;
        switch (splitType)
        {
            case SplitType.BranchX:
                bb0.max.X = midValue;
                bb1.min.X = midValue;
                break;
            case SplitType.BranchY:
                bb0.max.Y = midValue;
                bb1.min.Y = midValue;
                break;
        }
        var c0 = _Build(left, m, deep + 1, bb0);
        var c1 = _Build(m, right, deep + 1, bb1);
        var child = new Tree2Node()
        {
            left = left,
            right = right,
            childCount = right - left,
            midValue = midValue,
            childL = c0,
            childR = c1,
            splitType = splitType,
        };
        return child;
    }

    int PartitionX(int left, int right, float midValue)
    {
        if (left == right)
            return left;
        for (int i = left; i < right; i++)
        {
            if (values[i].position.X < midValue)
            {
                (values[i], values[left]) = (values[left], values[i]);
                left++;
            }
        }
        return left;
    }
    int PartitionY(int left, int right, float midValue)
    {
        if (left == right)
            return left;
        for (int i = left; i < right; i++)
        {
            if (values[i].position.Y < midValue)
            {
                (values[i], values[left]) = (values[left], values[i]);
                left++;
            }
        }
        return left;
    }
}
