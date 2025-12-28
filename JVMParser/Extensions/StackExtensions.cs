namespace JVMParser.Extensions
{
    public static class StackExtensions
    {
        extension<T>(Stack<T> stack)
        {
            public void PushFixed(T item)
            {
                if (stack.Count >= stack.Capacity)
                {
                    throw new IndexOutOfRangeException();
                }
                
                stack.Push(item);
            }
        }

        public static TI Pop<TI>(this Stack<object?> stack)
        {
            return (TI)stack.Pop()!;
        }
    }
}