namespace JVMParser.Mock
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JVMMockClassAttribute : Attribute
    {
        public string ClassName { get; }
        public string? SuperClassName { get; }

        public JVMMockClassAttribute(string className, string? superClassName = Constants.OBJECT_TYPE_NAME)
        {
            ClassName = className;
            SuperClassName = superClassName;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class JVMMockFieldAttribute : Attribute
    {
        public string FieldName { get; }

        public JVMMockFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class JVMMockMethodAttribute : Attribute
    {
        public string MethodName { get; }

        public JVMMockMethodAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}