using System.Diagnostics.CodeAnalysis;

namespace JVMParser
{
    #region RawValue classes
    public class JVMConstantPool
    {
        public JVMConstantPoolTag Tag;
        public Dictionary<string, object> ExtraData;

        public JVMConstantPool(JVMConstantPoolTag tag, Dictionary<string, object> extraData)
        {
            Tag = tag;
            ExtraData = extraData;
        }

        public virtual bool TryResolvePoolByIndexProperty(JVMClassRaw jvmClassRaw, string indexPropertyName, [NotNullWhen(true)] out JVMConstantPool? pool)
        {
            if (
                ExtraData.TryGetValue(indexPropertyName, out var indexValueObj) &&
                indexValueObj is ushort indexValue &&
                jvmClassRaw.ConstantPools.Length >= indexValue)
            {
                pool = jvmClassRaw.ConstantPools[indexValue - 1];
                return true;
            }

            pool = null;
            return false;
        }
        
        public virtual JVMConstantPool ResolvePoolByIndexProperty(JVMClassRaw jvmClassRaw, string indexPropertyName)
        {
            return TryResolvePoolByIndexProperty(jvmClassRaw, indexPropertyName, out var pool)
                ? pool
                : throw new KeyNotFoundException();
        }

        public override string? ToString()
        {
            return $"{Tag}: {{ {string.Join(", ", ExtraData.Select(kv => $"{kv.Key} = {kv.Value}") ?? [])} }}";
        }
    }
    
    public class JVMValueConstantPool : JVMConstantPool
    {
        public object Value;

        public JVMValueConstantPool(JVMConstantPoolTag tag, object value)
            : base(tag, null!)
        {
            Value = value;
        }

        public override bool TryResolvePoolByIndexProperty(JVMClassRaw jvmClassRaw, string indexPropertyName, [NotNullWhen(true)] out JVMConstantPool? pool)
        {
            pool = null;
            return false;
        }

        public override JVMConstantPool ResolvePoolByIndexProperty(JVMClassRaw jvmClassRaw, string indexPropertyName)
        {
            throw new KeyNotFoundException();
        }

        public override string? ToString()
        {
            return $"{Tag}: {Value}";
        }
    }

    public class JVMFieldRaw
    {
        public JVMAccessFlag[] AccessFlags;
        public ushort NameIndex;
        public ushort DescriptorIndex;
        public JVMAttributeRaw[] Attributes;
    }

    public class JVMMethodRaw
    {
        public JVMAccessFlag[] AccessFlags;
        public ushort NameIndex;
        public ushort DescriptorIndex;
        public JVMAttributeRaw[] Attributes;
    }

    public class JVMAttributeRaw
    {
        public ushort AttributeNameIndex;
        public byte[] Data;
    }
    
    public class JVMClassRaw
    {
        public string Magic;
        public ushort MajorVersion;
        public ushort MinorVersion;
        /// <summary>
        /// Indexed from 1.
        /// </summary>
        public JVMConstantPool[] ConstantPools;
        public JVMAccessFlag[] AccessFlags;
        public ushort ThisClassIndex;
        public ushort SuperClassIndex;
        public ushort[] Interfaces;
        public JVMFieldRaw[] Fields;
        public JVMMethodRaw[] Methods;
        public JVMAttributeRaw[] Attributes;
        
        public bool TryResolvePoolByIndex(ushort propertyIndex, [NotNullWhen(true)] out JVMConstantPool? pool)
        {
            if (ConstantPools.Length >= propertyIndex)
            {
                pool = ConstantPools[propertyIndex - 1];
                return true;
            }

            pool = null;
            return false;
        }
        
        public JVMConstantPool ResolvePoolByIndex(ushort propertyIndex)
        {
            return ConstantPools[propertyIndex - 1];
        }
        
        public object ResolveValuePoolValueByIndex(ushort propertyIndex)
        {
            return ((JVMValueConstantPool)ConstantPools[propertyIndex - 1]).Value;
        }
        
        public T ResolveValuePoolValueByIndex<T>(ushort propertyIndex)
        {
            return (T)ResolveValuePoolValueByIndex(propertyIndex);
        }

        public string RecursivelyResolveConstantPool(ushort index)
        {
            var resolvedPool = ResolvePoolByIndex(index);
            while (true)
            {
                if (resolvedPool is JVMValueConstantPool valuePool)
                {
                    return valuePool.Value.ToString();
                }

                switch (resolvedPool.Tag)
                {
                    case JVMConstantPoolTag.CLASS:
                    case JVMConstantPoolTag.MODULE:
                    case JVMConstantPoolTag.PACKAGE:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.NAME_INDEX);
                        continue;
                    case JVMConstantPoolTag.STRING:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.STRING_INDEX);
                        continue;
                    case JVMConstantPoolTag.METHOD_TYPE:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.DESCRIPTOR_INDEX);
                        continue;
                    case JVMConstantPoolTag.FIELD_REF:
                    case JVMConstantPoolTag.METHOD_REF:
                    case JVMConstantPoolTag.INTERFACE_METHOD_REF:
                    case JVMConstantPoolTag.NAME_AND_TYPE:
                    case JVMConstantPoolTag.METHOD_HANDLE:
                    case JVMConstantPoolTag.DYNAMIC:
                    case JVMConstantPoolTag.INVOKE_DYNAMIC:
                    case JVMConstantPoolTag._DUMMY:
                    case JVMConstantPoolTag.UTF8:
                    case JVMConstantPoolTag.INTEGER:
                    case JVMConstantPoolTag.FLOAT:
                    case JVMConstantPoolTag.LONG:
                    case JVMConstantPoolTag.DOUBLE:
                    default:
                        return $"[UNRESOLVABLE] {resolvedPool}";
                }
            }
        }
    }
    #endregion

    #region Processed value classes
    #region Descriptors
    public interface IJVMDescriptor
    {
        string ToDescriptorString();
        string ToString(string name);
    }
    
    public abstract class AJVMFieldDescriptor : IJVMDescriptor
    {
        public static AJVMFieldDescriptor ParseDescriptor(ref string descriptorString)
        {
            if (descriptorString.Length < 1)
            {
                throw new ArgumentException("Descriptor is empty!");
            }

            var firstChar = descriptorString[0];
            descriptorString = descriptorString[1..];
            switch (firstChar)
            {
                case 'B':
                    return new JVMFieldDescriptor(JVMFieldType.BYTE);
                case 'C':
                    return new JVMFieldDescriptor(JVMFieldType.CHAR);
                case 'D':
                    return new JVMFieldDescriptor(JVMFieldType.DOUBLE);
                case 'F':
                    return new JVMFieldDescriptor(JVMFieldType.FLOAT);
                case 'I':
                    return new JVMFieldDescriptor(JVMFieldType.INT);
                case 'J':
                    return new JVMFieldDescriptor(JVMFieldType.LONG);
                case 'S':
                    return new JVMFieldDescriptor(JVMFieldType.SHORT);
                case 'Z':
                    return new JVMFieldDescriptor(JVMFieldType.BOOL);
                case '[':
                    return new JVMArrayDescriptor(ParseDescriptor(ref descriptorString));
                case 'L':
                    var classEndIndex = descriptorString.IndexOf(';');
                    if (classEndIndex == -1)
                    {
                        throw new ArgumentException("No class field end character!");
                    }

                    var className = descriptorString[..classEndIndex];
                    descriptorString = descriptorString[(classEndIndex + 1)..];
                    return new JVMClassFieldDescriptor(className);
                default:
                    throw new ArgumentException("Unknown descriptor type!");
            }
        }

        public static AJVMFieldDescriptor ParseDescriptor(string descriptorString)
        {
            var res = ParseDescriptor(ref descriptorString);
            return descriptorString.Length == 0
                ? res
                : throw new ArgumentException($"Remaining descriptor data after parsing: \"{descriptorString}\"");
        }

        public abstract string ToDescriptorString();
        public abstract string ToString(string name);
    }
    
    public class JVMFieldDescriptor : AJVMFieldDescriptor
    {
        public readonly JVMFieldType FieldType;
        
        public JVMFieldDescriptor(JVMFieldType fieldType)
        {
            FieldType = fieldType;
        }

        public override string ToDescriptorString()
        {
            return FieldType switch
            {
                JVMFieldType.BYTE => "B",
                JVMFieldType.CHAR => "C",
                JVMFieldType.DOUBLE => "D",
                JVMFieldType.FLOAT => "F",
                JVMFieldType.INT => "I",
                JVMFieldType.LONG => "J",
                JVMFieldType.SHORT => "S",
                JVMFieldType.BOOL => "Z",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override string ToString(string fieldName)
        {
            return $"{FieldType.ToString().ToLower()} {fieldName}";
        }

        public override string? ToString()
        {
            return FieldType.ToString().ToLower();
        }
    }
    
    public class JVMClassFieldDescriptor : AJVMFieldDescriptor
    {
        public readonly string ClassName;
        
        public JVMClassFieldDescriptor(string className)
        {
            ClassName = className;
        }

        public override string ToDescriptorString()
        {
            return $"L{ClassName};";
        }

        public override string ToString(string fieldName)
        {
            return $"{ClassName} {fieldName}";
        }

        public override string? ToString()
        {
            return ClassName;
        }
    }
    
    public class JVMArrayDescriptor : AJVMFieldDescriptor
    {
        public readonly AJVMFieldDescriptor Field;
        
        public JVMArrayDescriptor(AJVMFieldDescriptor field)
        {
            Field = field;
        }

        public override string ToDescriptorString()
        {
            return '[' + Field.ToDescriptorString();
        }

        public override string ToString(string fieldName)
        {
            return $"{Field}[] {fieldName}";
        }

        public override string? ToString()
        {
            return $"{Field}[]";
        }
    }
    
    public class JVMMethodDescriptor : IJVMDescriptor
    {
        public readonly AJVMFieldDescriptor[] Parameters;
        public readonly AJVMFieldDescriptor? ReturnType;
        
        public JVMMethodDescriptor(string descriptorString)
        {
            if (!descriptorString.StartsWith('('))
            {
                throw new ArgumentException("Invalid method descriptor start!");
            }

            var paramsAndReturn = descriptorString.TrimStart('(').Split(')');
            if (paramsAndReturn.Length != 2)
            {
                throw new ArgumentException("No parameters and return part found in method descriptor!");
            }

            var returnDescriptor = paramsAndReturn[1];
            ReturnType = returnDescriptor != "V"
                ? AJVMFieldDescriptor.ParseDescriptor(returnDescriptor)
                : null;
            
            var paramDescriptors = paramsAndReturn[0];
            var parameters = new List<AJVMFieldDescriptor>();
            while (paramDescriptors.Length != 0)
            {
                parameters.Add(AJVMFieldDescriptor.ParseDescriptor(ref paramDescriptors));
            }
            Parameters = parameters.ToArray();
        }

        public string ToDescriptorString()
        {
            var parameters = string.Join("", Parameters.Select(p => p.ToDescriptorString()));
            return $"({parameters}){(ReturnType is not null ? ReturnType.ToDescriptorString() : "V")}";
        }

        public string ToString(string methodName = "Method")
        {
            return $"{(ReturnType is not null ? ReturnType : "void")} {methodName}({string.Join(", ", Parameters)});";
        }

        public override string? ToString()
        {
            return ToString();
        }
    }
    #endregion

    #region Attribute structs
    public class JVMInstruction
    {
        public JVMOpcode Opcode;
        public uint OriginalOffset;
        public object[] Arguments;

        public override string? ToString()
        {
            return $"{Opcode}{(Arguments.Length != 0 ? $": {string.Join(", ", Arguments)}" : "")}";
        }
    }
    
    public class JVMCode
    {
        public byte[] OriginalBytes;
        public JVMInstruction[] Instructions;

        public override string? ToString()
        {
            return OriginalBytes.ToString();
        }
    }
    
    public class JVMExceptionTable
    {
        public ushort StartPC;
        public ushort EndPC;
        public ushort HandlerPC;
        public string CatchTypeName;

        public override string? ToString()
        {
            return $"{CatchTypeName}: {StartPC}-{EndPC} -> {HandlerPC}";
        }
    }
    
    public class JVMCodeAttribute
    {
        public ushort MaxStack;
        public ushort MaxLocals;
        public JVMCode Code;
        public JVMExceptionTable[] ExceptionTables;
        public JVMAttribute[] Attributes;

        public override string? ToString()
        {
            return Code.ToString();
        }
    }

    public class JVMLineNumberTable
    {
        public ushort StartPC;
        public ushort LineNumber;

        public override string? ToString()
        {
            return $"{StartPC} -> {LineNumber}";
        }
    }

    public class JVMVerificationTypeInfo
    {
        public JVMVerificationType Tag;

        public JVMVerificationTypeInfo(JVMVerificationType tag)
        {
            Tag = tag;
        }

        public override string? ToString()
        {
            return Tag.ToString();
        }
    }

    public class JVMObjectVerificationTypeInfo : JVMVerificationTypeInfo
    {
        public string ClassName;

        public JVMObjectVerificationTypeInfo(string className)
            : base(JVMVerificationType.OBJECT)
        {
            ClassName = className;
        }

        public override string? ToString()
        {
            return $"{Tag}: {ClassName}";
        }
    }

    public class JVMUninitializedVerificationTypeInfo : JVMVerificationTypeInfo
    {
        public ushort Offset;

        public JVMUninitializedVerificationTypeInfo(ushort offset)
            : base(JVMVerificationType.UNINITIALIZED)
        {
            Offset = offset;
        }

        public override string? ToString()
        {
            return $"{Tag}: {Offset}";
        }
    }

    public class JVMStackMapFrame
    {
        public JVMStackMapFrameType FrameType;
        public byte FrameTypeNumber;

        public JVMStackMapFrame(JVMStackMapFrameType frameType, byte frameTypeNumber)
        {
            FrameType = frameType;
            FrameTypeNumber = frameTypeNumber;
        }

        public override string? ToString()
        {
            return FrameType.ToString();
        }
    }
    
    public class JVMStackMapFrameWithVerification : JVMStackMapFrame
    {
        public JVMVerificationTypeInfo[] Verifications;

        public JVMStackMapFrameWithVerification(
            JVMStackMapFrameType frameType,
            byte frameTypeNumber,
            JVMVerificationTypeInfo[] verifications)
            : base(frameType, frameTypeNumber)
        {
            Verifications = verifications;
        }
    }
    #endregion
    
    public class JVMAttribute
    {
        public string Name;
        public object Data;

        public override string? ToString()
        {
            return $"{Name} => {Data}";
        }
    }
        
    public class JVMField
    {
        public JVMAccessFlag[] AccessFlags;
        public string Name;
        public AJVMFieldDescriptor Descriptor;
        public JVMAttribute[] Attributes;

        public override string? ToString()
        {
            return $"{(AccessFlags.Length != 0 ? $"{string.Join(" ", AccessFlags).ToLower()} " : "")}{Descriptor.ToString(Name)}";
        }
    }

    public class JVMMethod
    {
        public JVMAccessFlag[] AccessFlags;
        public string Name;
        public JVMMethodDescriptor Descriptor;
        public JVMAttribute[] Attributes;

        public override string? ToString()
        {
            return $"{(AccessFlags.Length != 0 ? $"{string.Join(" ", AccessFlags).ToLower()} " : "")}{Descriptor.ToString(Name)}";
        }
    }
    
    public class JVMClass
    {
        public string Magic;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public JVMAccessFlag[] AccessFlags;
        public string ThisClass;
        public string? SuperClass;
        public string[] Interfaces;
        public JVMField[] Fields;
        public JVMMethod[] Methods;
        public JVMAttribute[] Attributes;

        public override string? ToString()
        {
            var accessFlagsStr = AccessFlags.Length != 0 ? $"{string.Join(" ", AccessFlags).ToLower()} " : "";
            return $"{accessFlagsStr}{ThisClass}{(SuperClass is not null ? $" : {SuperClass}" : "")} (v{MajorVersion}.{MinorVersion})";
        }
    }
    #endregion
}