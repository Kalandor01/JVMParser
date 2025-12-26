using System.Buffers.Binary;
using System.Numerics;

namespace JVMParser.Extensions
{
    public static class StreamExtension
    {
        extension(Stream stream)
        {
            public byte[] ReadBytes(int count)
            {
                var bytes = new byte[count];
                return stream.Read(bytes) >= count
                    ? bytes
                    : throw new EndOfStreamException();
            }

            public string ReadBytesAsHexString(int count)
            {
                return BitConverter.ToString(stream.ReadBytes(count));
            }

            public byte ReadByteB()
            {
                return (byte)stream.ReadByte();
            }

            public ushort ReadUInt16()
            {
                return BinaryPrimitives.ReadUInt16BigEndian(stream.ReadBytes(2));
            }

            public uint ReadUInt32()
            {
                return BinaryPrimitives.ReadUInt32BigEndian(stream.ReadBytes(4));
            }

            public int ReadInt32()
            {
                return BinaryPrimitives.ReadInt32BigEndian(stream.ReadBytes(4));
            }

            public float ReadFloat()
            {
                return BinaryPrimitives.ReadSingleBigEndian(stream.ReadBytes(4));
            }

            public ulong ReadUInt64()
            {
                return BinaryPrimitives.ReadUInt64BigEndian(stream.ReadBytes(8));
            }

            public long ReadInt64()
            {
                return BinaryPrimitives.ReadInt32BigEndian(stream.ReadBytes(8));
            }

            public double ReadDouble()
            {
                return BinaryPrimitives.ReadDoubleBigEndian(stream.ReadBytes(8));
            }

            public T[] ParseArray<T, TN>(TN count, Func<Stream, T> itemProcessor)
                where TN : INumber<TN>
            {
                var list = new List<T>();
                for (var x = TN.Zero; x < count; x++)
                {
                    var item = itemProcessor(stream);
                    list.Add(item);
                }
                return list.ToArray();
            }

            public T[] ParseArray<T>(Func<Stream, T> itemProcessor)
            {
                return stream.ParseArray(stream.ReadUInt16(), itemProcessor);
            }
        }
    }
}