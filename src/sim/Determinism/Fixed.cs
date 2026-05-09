using System;

namespace RtsGame.Sim.Determinism
{
    public readonly struct Fixed : IComparable<Fixed>, IEquatable<Fixed>
    {
        public const int FractionalBits = 16;
        public const long OneRaw = 1L << FractionalBits;

        public long Raw { get; }

        public Fixed(long raw)
        {
            Raw = raw;
        }

        public static Fixed FromInt(int value)
        {
            return new Fixed(checked((long)value * OneRaw));
        }

        public static Fixed FromRatio(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new DivideByZeroException("Fixed denominator cannot be zero.");
            }

            return new Fixed(checked(((long)numerator * OneRaw) / denominator));
        }

        public int FloorToInt()
        {
            if (Raw >= 0)
            {
                return (int)(Raw / OneRaw);
            }

            return (int)-(((-Raw) + OneRaw - 1) / OneRaw);
        }

        public static Fixed operator +(Fixed left, Fixed right)
        {
            return new Fixed(checked(left.Raw + right.Raw));
        }

        public static Fixed operator -(Fixed left, Fixed right)
        {
            return new Fixed(checked(left.Raw - right.Raw));
        }

        public static Fixed operator *(Fixed left, Fixed right)
        {
            return new Fixed(checked((left.Raw * right.Raw) / OneRaw));
        }

        public static Fixed operator /(Fixed left, Fixed right)
        {
            if (right.Raw == 0)
            {
                throw new DivideByZeroException("Fixed divisor cannot be zero.");
            }

            return new Fixed(checked((left.Raw * OneRaw) / right.Raw));
        }

        public static bool operator <=(Fixed left, Fixed right)
        {
            return left.Raw <= right.Raw;
        }

        public static bool operator >=(Fixed left, Fixed right)
        {
            return left.Raw >= right.Raw;
        }

        public static bool operator <(Fixed left, Fixed right)
        {
            return left.Raw < right.Raw;
        }

        public static bool operator >(Fixed left, Fixed right)
        {
            return left.Raw > right.Raw;
        }

        public int CompareTo(Fixed other)
        {
            return Raw.CompareTo(other.Raw);
        }

        public bool Equals(Fixed other)
        {
            return Raw == other.Raw;
        }

        public override bool Equals(object? obj)
        {
            return obj is Fixed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }

        public override string ToString()
        {
            return Raw.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
