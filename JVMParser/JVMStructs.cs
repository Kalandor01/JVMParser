using System.Diagnostics.CodeAnalysis;

namespace JVMParser
{
    #region RawValue classes
    public class JVMConstantPoolRaw
    {
        public JVMConstantPoolTag Tag;
        public Dictionary<string, object> ExtraData;

        public JVMConstantPoolRaw(JVMConstantPoolTag tag, Dictionary<string, object> extraData)
        {
            Tag = tag;
            ExtraData = extraData;
        }

        public bool TryResolvePoolByIndexProperty(JVMConstantPoolRaw[] rawPools, string indexPropertyName, [NotNullWhen(true)] out JVMConstantPoolRaw? pool)
        {
            if (
                ExtraData.TryGetValue(indexPropertyName, out var indexValueObj) &&
                indexValueObj is ushort indexValue &&
                rawPools.Length >= indexValue)
            {
                pool = rawPools[indexValue - 1];
                return true;
            }

            pool = null;
            return false;
        }
        
        public JVMConstantPoolRaw ResolvePoolByIndexProperty(JVMConstantPoolRaw[] rawPools, string indexPropertyName)
        {
            return TryResolvePoolByIndexProperty(rawPools, indexPropertyName, out var pool)
                ? pool
                : throw new KeyNotFoundException();
        }
        
        public object ResolveValuePoolValueByIndexProperty(JVMConstantPoolRaw[] rawPools, string indexPropertyName)
        {
            return ResolvePoolByIndexProperty(rawPools, indexPropertyName).ExtraData[Constants.ConstantPoolExtraPropertyName.VALUE];
        }
        
        public T ResolveValuePoolValueByIndexProperty<T>(JVMConstantPoolRaw[] rawPools, string indexPropertyName)
        {
            return (T)ResolveValuePoolValueByIndexProperty(rawPools, indexPropertyName);
        }

        public override string? ToString()
        {
            return Tag == JVMConstantPoolTag._DUMMY
                ? "[DUMMY CONSTANT POOL]"
                : $"{Tag}: {{ {string.Join(", ", ExtraData.Select(kv => $"{kv.Key} = {kv.Value}") ?? [])} }}";
        }
        
        public AJVMConstantPool ResolveConstantPool(JVMConstantPoolRaw[] rawPools)
        {
            return Tag switch
            {
                JVMConstantPoolTag.UTF8 or JVMConstantPoolTag.INTEGER or JVMConstantPoolTag.FLOAT
                    or JVMConstantPoolTag.LONG or JVMConstantPoolTag.DOUBLE
                    => new JVMValueConstantPool(Tag, ExtraData["value"]),
                JVMConstantPoolTag.CLASS or JVMConstantPoolTag.MODULE or JVMConstantPoolTag.PACKAGE => new JVMValueConstantPool(
                        Tag,
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.NAME_INDEX)
                    ),
                JVMConstantPoolTag.STRING => new JVMValueConstantPool(
                        Tag,
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.STRING_INDEX)
                    ),
                JVMConstantPoolTag.FIELD_REF or JVMConstantPoolTag.METHOD_REF or JVMConstantPoolTag.INTERFACE_METHOD_REF => new JVMRefConstantPool(
                        Tag,
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.CLASS_INDEX),
                        (JVMNameTypeConstantPool)ResolveConstantPoolByProperty(rawPools, Constants.ConstantPoolExtraPropertyName.NAME_AND_TYPE_INDEX)
                    ),
                JVMConstantPoolTag.NAME_AND_TYPE => new JVMNameTypeConstantPool(
                        Tag,
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.NAME_INDEX),
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.DESCRIPTOR_INDEX)
                    ),
                JVMConstantPoolTag.METHOD_HANDLE => new JVMHandleConstantPool(
                        Tag,
                        (JVMReferenceKind)ExtraData[Constants.ConstantPoolExtraPropertyName.REFERENCE_KIND],
                        (JVMRefConstantPool)ResolveConstantPoolByProperty(rawPools, Constants.ConstantPoolExtraPropertyName.REFERENCE_INDEX)
                    ),
                JVMConstantPoolTag.METHOD_TYPE => new JVMValueConstantPool(
                        Tag,
                        ResolveConstantPoolValueByProperty<string>(rawPools, Constants.ConstantPoolExtraPropertyName.DESCRIPTOR_INDEX)
                    ),
                JVMConstantPoolTag.DYNAMIC or JVMConstantPoolTag.INVOKE_DYNAMIC => new JVMDynamicConstantPool(
                        Tag,
                        (ushort)ExtraData[Constants.ConstantPoolExtraPropertyName.BOOTSTRAP_METHOD_ATTRIBUTE_INDEX],
                        (JVMNameTypeConstantPool)ResolveConstantPoolByProperty(rawPools, Constants.ConstantPoolExtraPropertyName.NAME_AND_TYPE_INDEX)
                    ),
                JVMConstantPoolTag._DUMMY => new JVMDummyConstantPool(),
                _ => throw new ArgumentOutOfRangeException(nameof(Tag), Tag, null),
            };
        }

        public AJVMConstantPool ResolveConstantPoolByProperty(JVMConstantPoolRaw[] rawPools, string indexPropertyName)
        {
            return ResolvePoolByIndexProperty(rawPools, indexPropertyName).ResolveConstantPool(rawPools);
        }

        public T ResolveConstantPoolValueByProperty<T>(JVMConstantPoolRaw[] rawPools, string indexPropertyName)
        {
            return (T)((JVMValueConstantPool)ResolveConstantPoolByProperty(rawPools, indexPropertyName)).Value;
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
        public AJVMConstantPool[] ConstantPools;
        public JVMAccessFlag[] AccessFlags;
        public ushort ThisClassIndex;
        public ushort SuperClassIndex;
        public ushort[] Interfaces;
        public JVMFieldRaw[] Fields;
        public JVMMethodRaw[] Methods;
        public JVMAttributeRaw[] Attributes;
        
        public AJVMConstantPool ResolvePoolByIndex(ushort poolIndex)
        {
            return ConstantPools[poolIndex - 1];
        }
        
        public T ResolvePoolByIndex<T>(ushort poolIndex)
            where T : AJVMConstantPool
        {
            return (T)ResolvePoolByIndex(poolIndex);
        }
        
        public object ResolveValuePoolValueByIndex(ushort poolIndex)
        {
            return ResolvePoolByIndex<JVMValueConstantPool>(poolIndex).Value;
        }
        
        public T ResolveValuePoolValueByIndex<T>(ushort poolIndex)
        {
            return ResolvePoolByIndex<JVMValueConstantPool>(poolIndex).ValueAs<T>();
        }
    }
    #endregion

    #region Processed value classes
    #region Constant pools
    public abstract class AJVMConstantPool
    {
        public readonly JVMConstantPoolTag Tag;

        protected AJVMConstantPool(JVMConstantPoolTag tag)
        {
            Tag = tag;
        }

        public override string? ToString()
        {
            return Tag.ToString();
        }
    }
    
    public class JVMDummyConstantPool : AJVMConstantPool
    {
        public JVMDummyConstantPool() : base(JVMConstantPoolTag._DUMMY) { }

        public override string? ToString()
        {
            return $"[DUMMY CONSTANT POOL]";
        }
    }
    
    public class JVMValueConstantPool : AJVMConstantPool
    {
        public readonly object Value;

        public JVMValueConstantPool(JVMConstantPoolTag tag, object value)
            : base(tag)
        {
            Value = value;
        }

        public T ValueAs<T>()
        {
            return (T)Value;
        }

        public override string? ToString()
        {
            return $"{Tag}: {Value}";
        }
    }
    
    public class JVMNameTypeConstantPool : AJVMConstantPool
    {
        public readonly string Name;
        public readonly IJVMDescriptor Descriptor;
    
        public JVMNameTypeConstantPool(JVMConstantPoolTag tag, string name, string descriptor)
            : base(tag)
        {
            Name = name;
            Descriptor = IJVMDescriptor.ParseDescriptor(descriptor);
        }
    
        public string? ToString(bool includeTag)
        {
            return $"{(includeTag ? $"{Tag}: " : "")}{Descriptor.ToString(Name)}";
        }
    
        public override string? ToString()
        {
            return ToString(true);
        }
    }
    
    public class JVMRefConstantPool : AJVMConstantPool
    {
        public readonly string ClassName;
        public readonly JVMNameTypeConstantPool NameAndType;
    
        public JVMRefConstantPool(JVMConstantPoolTag tag, string className, JVMNameTypeConstantPool nameAndType)
            : base(tag)
        {
            ClassName = className;
            NameAndType = nameAndType;
        }
    
        public string? ToString(bool includeTag)
        {
            return $"{(includeTag ? $"{Tag}: " : "")}{ClassName} -> {NameAndType.ToString(false)}";
        }
    
        public override string? ToString()
        {
            return ToString(true);
        }
    }
    
    public class JVMHandleConstantPool : AJVMConstantPool
    {
        public readonly JVMReferenceKind Kind;
        public readonly JVMRefConstantPool Reference;
    
        public JVMHandleConstantPool(JVMConstantPoolTag tag, JVMReferenceKind referenceKind, JVMRefConstantPool reference)
            : base(tag)
        {
            Kind = referenceKind;
            Reference = reference;
        }
    
        public override string? ToString()
        {
            return $"{Tag}: {Kind}: {Reference.ToString(false)}";
        }
    }
    
    public class JVMDynamicConstantPool : AJVMConstantPool
    {
        public readonly ushort BootstrapMethodAttributeIndex;
        public readonly JVMNameTypeConstantPool NameAndType;
    
        public JVMDynamicConstantPool(JVMConstantPoolTag tag, ushort bootstrapIndex, JVMNameTypeConstantPool nameAndType)
            : base(tag)
        {
            BootstrapMethodAttributeIndex = bootstrapIndex;
            NameAndType = nameAndType;
        }
    
        public override string? ToString()
        {
            return $"{Tag}: ({BootstrapMethodAttributeIndex}), {NameAndType.ToString(false)}";
        }
    }
    #endregion
    
    #region Descriptors
    public interface IJVMDescriptor
    {
        string ToDescriptorString();
        string ToString(string name);
        
        public static IJVMDescriptor ParseDescriptor(string descriptorString)
        {
            if (descriptorString.StartsWith('('))
            {
                return new JVMMethodDescriptor(descriptorString);
            }
            
            var fieldDescriptor = AJVMFieldDescriptor.ParseFieldDescriptor(ref descriptorString);
            return descriptorString.Length == 0
                ? fieldDescriptor
                : throw new ArgumentException($"Remaining descriptor data after parsing: \"{descriptorString}\"");
        }
    }
    
    public abstract class AJVMFieldDescriptor : IJVMDescriptor
    {
        public static AJVMFieldDescriptor ParseFieldDescriptor(ref string descriptorString)
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
                    return new JVMArrayDescriptor(ParseFieldDescriptor(ref descriptorString));
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

        public static AJVMFieldDescriptor ParseFieldDescriptor(string descriptorString)
        {
            var res = ParseFieldDescriptor(ref descriptorString);
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
                ? AJVMFieldDescriptor.ParseFieldDescriptor(returnDescriptor)
                : null;
            
            var paramDescriptors = paramsAndReturn[0];
            var parameters = new List<AJVMFieldDescriptor>();
            while (paramDescriptors.Length != 0)
            {
                parameters.Add(AJVMFieldDescriptor.ParseFieldDescriptor(ref paramDescriptors));
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
            return Instructions.ToString();
        }
    }
    
    public class JVMExceptionTable
    {
        public ushort StartPC;
        public ushort EndPC;
        public ushort HandlerPC;
        public string? CatchTypeName;

        public override string? ToString()
        {
            return $"{CatchTypeName ?? "Exception"}: {StartPC}-{EndPC} -> {HandlerPC}";
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