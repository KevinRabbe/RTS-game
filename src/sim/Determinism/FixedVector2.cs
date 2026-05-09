namespace RtsGame.Sim.Determinism
{
    public readonly struct FixedVector2
    {
        public Fixed X { get; }
        public Fixed Y { get; }

        public FixedVector2(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        public static FixedVector2 FromInts(int x, int y)
        {
            return new FixedVector2(Fixed.FromInt(x), Fixed.FromInt(y));
        }

        public static FixedVector2 operator +(FixedVector2 left, FixedVector2 right)
        {
            return new FixedVector2(left.X + right.X, left.Y + right.Y);
        }

        public static FixedVector2 operator -(FixedVector2 left, FixedVector2 right)
        {
            return new FixedVector2(left.X - right.X, left.Y - right.Y);
        }

        public static FixedVector2 Multiply(FixedVector2 value, Fixed scalar)
        {
            return new FixedVector2(value.X * scalar, value.Y * scalar);
        }

        public long LengthSquaredRaw()
        {
            checked
            {
                long x = X.Raw;
                long y = Y.Raw;
                return x * x + y * y;
            }
        }

        public static Fixed Distance(FixedVector2 from, FixedVector2 to)
        {
            return new Fixed(DeterministicMath.SqrtRaw((to - from).LengthSquaredRaw()));
        }
    }
}
