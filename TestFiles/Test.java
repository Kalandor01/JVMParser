package TestFiles;

public class Test extends ATest implements ITest
{
    public double[] testField = new double[] { 5, 2, -5.735};

    public Test(double num) {
        super(num);
    }

    private int[] Bar()
    {
        return new int[3];
    }

    public int[] Bar(int ha)
    {
        return new int[3];
    }

    public static String Foo(int intVal, String strVal)
    {
        return "test";
    }

    public static void main(String[] args)
    {
        System.out.println("Hello World!");
        var localTest = new Test(69);
        var res = localTest.Bar();
        localTest.AbstractImplement();
    }

    @Override
    public String[] ImplementMethod() {
        try
        {
            throw new UnsupportedOperationException("Unimplemented method 'ImplementMethod'");
        }
        catch (ArrayIndexOutOfBoundsException ex)
        {
            return new String[0];
        }
    }

    @Override
    public void AbstractImplement() {
        // TODO Auto-generated method stub
        throw new UnsupportedOperationException("Unimplemented method 'AbstractImplement'");
    }
}
