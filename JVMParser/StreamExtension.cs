using System.Buffers.Binary;

namespace JVMParser;

public static class StreamExtension
{
    public static byte[] ReadBytes(this Stream stream, int count)
    {
        var bytes = new byte[count];
        return stream.Read(bytes) >= count
            ? bytes
            : throw new EndOfStreamException();
    }

    public static string ReadBytesAsHexString(this Stream stream, int count)
    {
        return BitConverter.ToString(stream.ReadBytes(count));
    }

    public static ushort ReadUInt16(this Stream stream)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(stream, 2));
    }

    public static uint ReadUInt32(this Stream stream)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(stream, 4));
    }

    public static int ReadInt32(this Stream stream)
    {
        return BinaryPrimitives.ReadInt32BigEndian(ReadBytes(stream, 4));
    }

    public static float ReadFloat(this Stream stream)
    {
        return BinaryPrimitives.ReadSingleBigEndian(ReadBytes(stream, 4));
    }

    public static ulong ReadUInt64(this Stream stream)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(stream, 8));
    }

    public static long ReadInt64(this Stream stream)
    {
        return BinaryPrimitives.ReadInt32BigEndian(ReadBytes(stream, 8));
    }

    public static double ReadDouble(this Stream stream)
    {
        return BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(stream, 8));
    }
}