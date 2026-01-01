namespace JVMParser.Mock
{
    public class JVMMockClasses
    {
        [JVMMockClass("java/lang/System")]
        public class System : IJVMMockClass
        {
            public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC, JVMAccessFlag.FINAL];
            
            [JVMMockField("out")]
            public static class Out
            {
                public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC, JVMAccessFlag.STATIC, JVMAccessFlag.FINAL];
                public const string DESCRIPTOR = "Ljava/io/PrintStream;";
            }
        }

        [JVMMockClass("java/io/PrintStream")]
        public class PrintStream : IJVMMockClass
        {
            public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC];
            
            [JVMMockMethod("println")]
            public static class PrintLine
            {
                public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC];
                public const string DESCRIPTOR = "(Ljava/lang/String;)V";
                public static readonly JVMExternalMethod EXTERNAL_METHOD = JVMMock.ToExternalMethod<string>(ExternalMethods.MockPrintLine);
            }
        }

        [JVMMockClass("java/lang/invoke/StringConcatFactory")]
        public class StringConcatFactory : IJVMMockClass
        {
            public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC, JVMAccessFlag.FINAL];
            
            [JVMMockMethod("makeConcatWithConstants")]
            public static class MakeConcatWithConstants
            {
                public static readonly IReadOnlyCollection<JVMAccessFlag> ACCESS_FLAGS = [JVMAccessFlag.PUBLIC, JVMAccessFlag.STATIC];
                public const string DESCRIPTOR =
                    "(Ljava/lang/invoke/MethodHandles$Lookup;Ljava/lang/String;Ljava/lang/invoke/MethodType;Ljava/lang/String;[Ljava/lang/Object;)Ljava/lang/invoke/CallSite;";
                public static readonly JVMExternalMethod EXTERNAL_METHOD = JVMMock.ToExternalMethod<object, string, object, string, object[], object>(
                    ExternalMethods.MockMakeConcatWithConstants
                );
            }
        }
    }

    internal static class ExternalMethods
    {
        public static void MockPrintLine(JVMClass[] references, string text)
        {
            Console.WriteLine(text);
        }
        
        public static object MockMakeConcatWithConstants(JVMClass[] references, object lookup, string str1, object methodType, string str2, object[] args)
        {
            // java/lang/invoke/MethodHandles$Lookup
            var l = lookup;
            
            // java/lang/invoke/MethodType
            var m = methodType;
            
            // java/lang/invoke/CallSite
            return "???";
        }
    }
}