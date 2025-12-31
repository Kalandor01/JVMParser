using JVMParser.JVMClasses;

namespace JVMParser
{
    public delegate bool JVMExternalMethod(JVMClass[] references, object?[] args, out object? returnValue);
    
    public class JVMMock
    {
        #region Public methods
        public static JVMClass MockSystemClass()
        {
            return MockClass(
                Constants.MockClass.System.CLASS_NAME,
                [JVMAccessFlag.PUBLIC, JVMAccessFlag.FINAL],
                [MockOutField()],
                []
            );
        }
        
        public static JVMClass MockPrintStreamClass()
        {
            return MockClass(
                Constants.MockClass.PrintStream.CLASS_NAME,
                [JVMAccessFlag.PUBLIC],
                [],
                [MockPrintLineMethod()]
            );
        }
        
        public static JVMClass MockStringConcatFactoryClass()
        {
            return MockClass(
                Constants.MockClass.StringConcatFactory.CLASS_NAME,
                [JVMAccessFlag.PUBLIC, JVMAccessFlag.FINAL],
                [],
                [MockMakeConcatWithConstantsMethod()]
            );
        }
        #endregion

        #region Private methods
        private static JVMClass MockClass(
            string name,
            JVMAccessFlag[] accessFlags,
            JVMField[] fields,
            JVMMethod[] methods,
            string? superName = Constants.OBJECT_TYPE_NAME,
            string[]? interfaces = null
        )
        {
            var systemClass = new JVMClass
            {
                Magic = Constants.MockClass.MAGIC_HEX_STRING,
                MajorVersion = Constants.MockClass.MAJOR_VERSION,
                MinorVersion = Constants.MockClass.MINOR_VERSION,
                AccessFlags = accessFlags,
                ThisClass = name,
                SuperClass = superName,
                Interfaces = interfaces ?? [],
                Fields = fields,
                Methods = methods,
                Attributes = [],
            };
            return systemClass;
        }
        
        private static JVMField MockField(string name, string descriptor, JVMAccessFlag[] accessFlags)
        {
            var field = new JVMField
            {
                AccessFlags = accessFlags,
                Name = name,
                Descriptor = Descriptors.AJVMFieldDescriptor.ParseFieldDescriptor(descriptor),
                Attributes = [],
            };
            return field;
        }
        
        private static JVMMethod MockMethod(string name, string descriptor, JVMAccessFlag[] accessFlags, JVMExternalMethod externalMethod)
        {
            var method = new JVMMethod
            {
                AccessFlags = accessFlags,
                Name = name,
                Descriptor = new Descriptors.JVMMethodDescriptor(descriptor),
                Attributes = [
                    new JVMAttribute
                    {
                        Name = Constants.AttributeName._EXTERNAL_METHOD_MARKER,
                        Data = externalMethod,
                    },
                ],
            };
            return method;
        }
        
        private static JVMField MockOutField()
        {
            return MockField(
                Constants.MockClass.System.OutField.NAME,
                Constants.MockClass.System.OutField.DESCRIPTOR,
                [JVMAccessFlag.PUBLIC, JVMAccessFlag.STATIC, JVMAccessFlag.FINAL]
            );
        }
        
        private static JVMMethod MockPrintLineMethod()
        {
            return MockMethod(
                Constants.MockClass.PrintStream.PrintLineMethod.NAME,
                Constants.MockClass.PrintStream.PrintLineMethod.DESCRIPTOR,
                [JVMAccessFlag.PUBLIC],
                MockPrintLine
            );
        }

        private static JVMMethod MockMakeConcatWithConstantsMethod()
        {
            return MockMethod(
                Constants.MockClass.StringConcatFactory.MakeConcatWithConstants.NAME,
                Constants.MockClass.StringConcatFactory.MakeConcatWithConstants.DESCRIPTOR,
                [JVMAccessFlag.PUBLIC, JVMAccessFlag.STATIC],
                MockMakeConcatWithConstants
            );
        }
        
        #region External methods
        private static bool MockPrintLine(JVMClass[] references, object?[] args, out object? returnValue)
        {
            Console.WriteLine(args[0]);
            returnValue = null;
            return false;
        }
        
        private static bool MockMakeConcatWithConstants(JVMClass[] references, object?[] args, out object? returnValue)
        {
            returnValue = null;
            return false;
        }
        #endregion
        #endregion
    }
}