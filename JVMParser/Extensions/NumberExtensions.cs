namespace JVMParser.Extensions;

public static class NumberExtensions
{
    public static int SignExtend(this byte num)
    {
        return (sbyte)num;
    }
}