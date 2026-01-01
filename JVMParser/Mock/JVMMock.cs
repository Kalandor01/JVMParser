using System.Reflection;
using JVMParser.JVMClasses;

namespace JVMParser.Mock
{
    public delegate bool JVMExternalMethod(JVMClass[] references, object?[] args, out object? returnValue);
    
    public static class JVMMock
    {
        #region Public methods
        public static JVMField MockField<TF>()
        {
            return TryMockField(typeof(TF)) ?? throw new ArgumentException(null, nameof(TF));
        }
        
        public static JVMMethod MockMethod<TM>()
        {
            return TryMockMethod(typeof(TM)) ?? throw new ArgumentException(null, nameof(TM));
        }

        public static JVMClass MockClass<TC>()
            where TC : IJVMMockClass
        {
            var classType = typeof(TC);
            if (classType.GetCustomAttribute<JVMMockClassAttribute>() is not { } classAttribute)
            {
                throw new ArgumentException(nameof(JVMMockClassAttribute));
            }

            var accessFlags = GetMockAccessFlags(classType);
            var interfaces = GetMockInterfaces(classType);

            var nestedClasses = classType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            var fields = nestedClasses
                .Select(TryMockField)
                .Where(f => f is not null)
                .Cast<JVMField>()
                .ToArray();
            
            var methods = nestedClasses
                .Select(TryMockMethod)
                .Where(m => m is not null)
                .Cast<JVMMethod>()
                .ToArray();

            return MockClass(
                classAttribute.ClassName,
                accessFlags,
                fields,
                methods,
                classAttribute.SuperClassName,
                interfaces
            );
        }

        #region ToExternalMethod methods
        public static JVMExternalMethod ToExternalMethod<TA>(Action<JVMClass[], TA> method)
        {
            return (references, args, out value) =>
            {
                method(references, (TA)args[0]!);
                value = null;
                return false;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2>(Action<JVMClass[], TA1, TA2> method)
        {
            return (references, args, out value) =>
            {
                method(references, (TA1)args[0]!, (TA2)args[1]!);
                value = null;
                return false;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3>(Action<JVMClass[], TA1, TA2, TA3> method)
        {
            return (references, args, out value) =>
            {
                method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!);
                value = null;
                return false;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3, TA4>(Action<JVMClass[], TA1, TA2, TA3, TA4> method)
        {
            return (references, args, out value) =>
            {
                method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!, (TA4)args[3]!);
                value = null;
                return false;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3, TA4, TA5>(Action<JVMClass[], TA1, TA2, TA3, TA4, TA5> method)
        {
            return (references, args, out value) =>
            {
                method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!, (TA4)args[3]!, (TA5)args[4]!);
                value = null;
                return false;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA, TO>(Func<JVMClass[], TA, TO> method)
        {
            return (references, args, out value) =>
            {
                value = method(references, (TA)args[0]!);
                return true;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TO>(Func<JVMClass[], TA1, TA2, TO> method)
        {
            return (references, args, out value) =>
            {
                value = method(references, (TA1)args[0]!, (TA2)args[1]!);
                return true;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3, TO>(Func<JVMClass[], TA1, TA2, TA3, TO> method)
        {
            return (references, args, out value) =>
            {
                value = method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!);
                return true;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3, TA4, TO>(Func<JVMClass[], TA1, TA2, TA3, TA4, TO> method)
        {
            return (references, args, out value) =>
            {
                value = method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!, (TA4)args[3]!);
                return true;
            };
        }
        
        public static JVMExternalMethod ToExternalMethod<TA1, TA2, TA3, TA4, TA5, TO>(Func<JVMClass[], TA1, TA2, TA3, TA4, TA5, TO> method)
        {
            return (references, args, out value) =>
            {
                value = method(references, (TA1)args[0]!, (TA2)args[1]!, (TA3)args[2]!, (TA4)args[3]!, (TA5)args[4]!);
                return true;
            };
        }
        #endregion
        #endregion

        #region Private methods
        private static FT? TryGetMockClassField<FT>(Type mockClassType, string fieldName)
        {
            return (FT?)mockClassType
                .GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);
        }
        
        private static FT GetMockClassField<FT>(Type mockClassType, string fieldName)
        {
            return TryGetMockClassField<FT>(mockClassType, fieldName) ?? throw new ArgumentException(null, nameof(fieldName));
        }

        private static JVMAccessFlag[] GetMockAccessFlags(Type mockType)
        {
            return GetMockClassField<IReadOnlyCollection<JVMAccessFlag>>(
                    mockType,
                    Constants.MockClass.FieldNames.ACCESS_FLAGS
                )
                .ToArray();
        }

        private static string[] GetMockInterfaces(Type mockType)
        {
            return TryGetMockClassField<IReadOnlyCollection<string>>(
                    mockType,
                    Constants.MockClass.FieldNames.INTERFACES
                )
                ?.ToArray() ?? [];
        }

        private static string GetMockDescriptor(Type mockType)
        {
            return GetMockClassField<string>(mockType, Constants.MockClass.FieldNames.DESCRIPTOR);
        }

        private static JVMExternalMethod GetExternalMethod(Type mockType)
        {
            return GetMockClassField<JVMExternalMethod>(mockType, Constants.MockClass.FieldNames.EXTERNAL_METHOD);
        }
        
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

        private static JVMField? TryMockField(Type fieldType)
        {
            if (fieldType.GetCustomAttribute<JVMMockFieldAttribute>() is not { } fieldAttribute)
            {
                return null;
            }

            var accessFlags = GetMockAccessFlags(fieldType);
            var descriptor = GetMockDescriptor(fieldType);

            return MockField(
                fieldAttribute.FieldName,
                descriptor,
                accessFlags
            );
        }

        private static JVMMethod? TryMockMethod(Type methodType)
        {
            if (methodType.GetCustomAttribute<JVMMockMethodAttribute>() is not { } methodAttribute)
            {
                return null;
            }

            var accessFlags = GetMockAccessFlags(methodType);
            var descriptor = GetMockDescriptor(methodType);
            var externalMethod = GetExternalMethod(methodType);

            return MockMethod(
                methodAttribute.MethodName,
                descriptor,
                accessFlags,
                externalMethod
            );
        }
        #endregion
    }
}