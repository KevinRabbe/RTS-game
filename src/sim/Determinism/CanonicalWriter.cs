using System;
using System.Collections.Generic;

namespace RtsGame.Sim.Determinism
{
    public sealed class CanonicalWriter
    {
        private readonly List<byte> _bytes = new List<byte>();

        public byte[] ToArray()
        {
            return _bytes.ToArray();
        }

        public void WriteBool(bool value)
        {
            _bytes.Add(value ? (byte)1 : (byte)0);
        }

        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        public void WriteUInt16(ushort value)
        {
            _bytes.Add((byte)value);
            _bytes.Add((byte)(value >> 8));
        }

        public void WriteUInt32(uint value)
        {
            _bytes.Add((byte)value);
            _bytes.Add((byte)(value >> 8));
            _bytes.Add((byte)(value >> 16));
            _bytes.Add((byte)(value >> 24));
        }

        public void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        public void WriteUInt64(ulong value)
        {
            for (int i = 0; i < 8; i++)
            {
                _bytes.Add((byte)(value >> (i * 8)));
            }
        }

        public void WriteFixed(Fixed value)
        {
            WriteInt64(value.Raw);
        }

        public void WriteListCount(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "List count cannot be negative.");
            }

            WriteInt32(count);
        }
    }
}
