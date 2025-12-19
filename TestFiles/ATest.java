package TestFiles;

public abstract class ATest
{
    private final double PI1 = 3;
    private final double PI2 = 4;
    private final double PI3 = 5;
    private final double PI4 = 6;
    private final double PI5 = 7;

    private float privateNum;

    public double Num;

    public ATest(double num)
    {
        Num = num;
    }

    public abstract void AbstractImplement();
}
