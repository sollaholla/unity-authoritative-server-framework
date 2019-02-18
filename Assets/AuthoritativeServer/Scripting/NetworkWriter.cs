using System;
using System.Collections.Generic;

using UnityEngine;

namespace AuthoritativeServer
{
    /// <summary>
    /// A quick way of storing data and reading data from and into a byte array.
    /// </summary>
    public class NetworkWriter
    {
        private List<byte> m_Data;
        private byte[] m_DataArray;

        public NetworkWriter()
        {
            m_Data = new List<byte>();
        }

        public NetworkWriter(byte[] data) : this()
        {
            WriteBytes(data);
        }

        /// <summary>
        /// The position in the byte array for reading.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// The count of bytes in the writer.
        /// </summary>
        public int Count { get { return m_Data?.Count ?? 0; } }

        /// <summary>
        /// The count of remaining bytes to read.
        /// </summary>
        public int Remaining {
            get { return Read.Length - (Position + 1); }
        }

        private byte[] Read {
            get {
                if (m_DataArray == null)
                    m_DataArray = m_Data.ToArray();
                return m_DataArray;
            }
        }

        public void Write(int value)
        {
            m_Data.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(bool value)
        {
            m_Data.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(short value)
        {
            m_Data.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(long value)
        {
            m_Data.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(float value)
        {
            m_Data.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }

        public void Write(byte value)
        {
            m_Data.Add(value);
        }

        /// <summary>
        /// Writes only bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public void WriteBytes(byte[] bytes)
        {
            m_Data.AddRange(bytes);
        }

        /// <summary>
        /// Writes bytes and the size of the bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public void WriteBytesAndSize(byte[] bytes)
        {
            m_Data.AddRange(BitConverter.GetBytes((short)bytes.Length));
            m_Data.AddRange(bytes);
        }

        public int ReadInt32()
        {
            int value = BitConverter.ToInt32(Read, Position);
            Position += sizeof(int);
            return value;
        }

        public short ReadInt16()
        {
            short value = BitConverter.ToInt16(Read, Position);
            Position += sizeof(short);
            return value;
        }

        public float ReadSingle()
        {
            float value = BitConverter.ToSingle(Read, Position);
            Position += sizeof(float);
            return value;
        }

        public long ReadInt64()
        {
            long value = BitConverter.ToInt64(Read, Position);
            Position += sizeof(long);
            return value;
        }

        public byte ReadByte()
        {
            byte value = Read[Position];
            Position++;
            return value;
        }

        public bool ReadBool()
        {
            bool value = BitConverter.ToBoolean(Read, Position);
            Position += sizeof(bool);
            return value;
        }

        public Vector3 ReadVector3()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            return new Vector3(x, y, z);
        }

        public Vector2 ReadVector2()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads the remaining bytes in the network message.
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            int count = Read.Length - Position;
            byte[] read = new byte[count];
            Buffer.BlockCopy(Read, Position, read, 0, count);
            Position += count;
            return read;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] read = new byte[count];
            Buffer.BlockCopy(Read, Position, read, 0, count);
            Position += count;
            return read;
        }

        /// <summary>
        /// Returns a copy of our byte data.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return m_Data.ToArray();
        }
    }
}
