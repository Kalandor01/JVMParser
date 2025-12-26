using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using JVMParser.JVMClasses;

namespace JVMParser
{
    public class JVMParser
    {
        #region Public methods
        public static JVMClassRaw? Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            var stream = File.OpenRead(filePath);
            
            var jvmClass = new JVMClassRaw
            {
                Magic = stream.ReadBytesAsHexString(4),
                MinorVersion = stream.ReadUInt16(),
                MajorVersion = stream.ReadUInt16(),
                ConstantPools = GetConstantPools(stream),
                AccessFlags = GetAccessFlags(stream),
                ThisClassIndex = stream.ReadUInt16(),
                SuperClassIndex = stream.ReadUInt16(),
                Interfaces = GetArrayFromStream(stream, GetRawInterface),
                Fields = GetArrayFromStream(stream, GetRawField),
                Methods = GetArrayFromStream(stream, GetRawMethod),
                Attributes = GetArrayFromStream(stream, GetRawAttribute),
            };

            return stream.Position != stream.Length
                ? throw new EndOfStreamException()
                : jvmClass;
        }

        public static JVMClass RevolveJVMClass(JVMClassRaw jvmClassRaw)
        {
            var jvmClass = new JVMClass
            {
                Magic = jvmClassRaw.Magic,
                MajorVersion = jvmClassRaw.MajorVersion,
                MinorVersion = jvmClassRaw.MinorVersion,
                AccessFlags = jvmClassRaw.AccessFlags,
                ThisClass = jvmClassRaw.ResolveValuePoolValueByIndex<string>(jvmClassRaw.ThisClassIndex),
                SuperClass = jvmClassRaw.SuperClassIndex != 0
                    ? jvmClassRaw.ResolveValuePoolValueByIndex<string>(jvmClassRaw.SuperClassIndex)
                    : null,
                Interfaces = jvmClassRaw.Interfaces.Select(i => GetInterface(jvmClassRaw, i)).ToArray(),
                Fields = jvmClassRaw.Fields.Select(f => GetField(jvmClassRaw, f)).ToArray(),
                Methods = jvmClassRaw.Methods.Select(m => GetMethod(jvmClassRaw, m)).ToArray(),
                Attributes = jvmClassRaw.Attributes.Select(a => GetAttribute(jvmClassRaw, a)).ToArray(),
            };

            var classAttributes = jvmClass.Attributes;
            var fieldAttributes = jvmClass.Fields.SelectMany(f => f.Attributes).ToList();
            var methodAttributes = jvmClass.Methods.SelectMany(m => m.Attributes).ToList();

            var attributes = classAttributes
                .Concat(fieldAttributes)
                .Concat(methodAttributes)
                .ToList();

            attributes = attributes
                .Concat(attributes
                    .Where(a => a.Name == Constants.AttributeName.CODE)
                    .Select(a => (JVMCodeAttribute)a.Data)
                    .SelectMany(c => c.Attributes)
                )
                .ToList();
            
            return jvmClass;
        }
        #endregion

        #region Private methods
        #region Stream parsing
        private static T[] GetArrayFromStream<T, TN>(Stream stream, TN count, Func<Stream, T> itemProcessor)
            where TN : INumber<TN>
        {
            var list = new List<T>();
            for (var x = TN.Zero; x < count; x++)
            {
                var item = itemProcessor(stream);
                list.Add(item);
            }
            return list.ToArray();
        }
        
        private static T[] GetArrayFromStream<T>(Stream stream, Func<Stream, T> itemProcessor)
        {
            return GetArrayFromStream(stream, stream.ReadUInt16(), itemProcessor);
        }

        private static T[] GetArrayFromBytes<T>(byte[] bytes, Func<Stream, T> itemProcessor)
        {
            var stream = new MemoryStream(bytes);
            var array = GetArrayFromStream(stream, itemProcessor);
            return stream.Position == stream.Length
                ? array
                : throw new EndOfStreamException();
        }
        
        private static AJVMConstantPool[] GetConstantPools(Stream stream)
        {
            var constantPoolCount = stream.ReadUInt16();
            var constantPools = new List<JVMConstantPoolRaw>();
            for (var x = 1; x < constantPoolCount; x++)
            {
                var tag = (JVMConstantPoolTag)stream.ReadByteB();
                var constantPool = new JVMConstantPoolRaw(tag, GetJVMConstantPoolData(stream, tag, out var addExtra));
                constantPools.Add(constantPool);
                if (addExtra)
                {
                    constantPools.Add(new JVMConstantPoolRaw(JVMConstantPoolTag._DUMMY, null!));
                    x++;
                }
            }

            var rawPools = constantPools.ToArray();
            return rawPools
                .Select(p => p.ResolveConstantPool(rawPools))
                .ToArray();
        }
        
        private static Dictionary<string, object> GetJVMConstantPoolData(Stream stream, JVMConstantPoolTag tag, out bool addExtra)
        {
            addExtra = false;
            var extraData = new Dictionary<string, object>();
            switch (tag)
            {
                case JVMConstantPoolTag.UTF8:
                    var length = stream.ReadUInt16();
                    extraData[Constants.ConstantPoolExtraPropertyName.VALUE] = Encoding.UTF8.GetString(stream.ReadBytes(length));
                    return extraData;
                case JVMConstantPoolTag.INTEGER:
                    extraData[Constants.ConstantPoolExtraPropertyName.VALUE] = stream.ReadInt32();
                    return extraData;
                case JVMConstantPoolTag.FLOAT:
                    extraData[Constants.ConstantPoolExtraPropertyName.VALUE] = stream.ReadFloat();
                    return extraData;
                case JVMConstantPoolTag.LONG:
                    addExtra = true;
                    extraData[Constants.ConstantPoolExtraPropertyName.VALUE] = stream.ReadInt64();
                    return extraData;
                case JVMConstantPoolTag.DOUBLE:
                    addExtra = true;
                    extraData[Constants.ConstantPoolExtraPropertyName.VALUE] = stream.ReadDouble();
                    return extraData;
                case JVMConstantPoolTag.CLASS:
                case JVMConstantPoolTag.MODULE:
                case JVMConstantPoolTag.PACKAGE:
                    extraData[Constants.ConstantPoolExtraPropertyName.NAME_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.UTF8
                    return extraData;
                case JVMConstantPoolTag.STRING:
                    extraData[Constants.ConstantPoolExtraPropertyName.STRING_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.UTF8
                    return extraData;
                case JVMConstantPoolTag.FIELD_REF:
                case JVMConstantPoolTag.METHOD_REF:
                case JVMConstantPoolTag.INTERFACE_METHOD_REF:
                    extraData[Constants.ConstantPoolExtraPropertyName.CLASS_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.CLASS
                    extraData[Constants.ConstantPoolExtraPropertyName.NAME_AND_TYPE_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.NAME_AND_TYPE
                    return extraData;
                case JVMConstantPoolTag.NAME_AND_TYPE:
                    extraData[Constants.ConstantPoolExtraPropertyName.NAME_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.UTF8
                    extraData[Constants.ConstantPoolExtraPropertyName.DESCRIPTOR_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.UTF8
                    return extraData;
                case JVMConstantPoolTag.METHOD_HANDLE:
                    extraData[Constants.ConstantPoolExtraPropertyName.REFERENCE_KIND] = (JVMReferenceKind)stream.ReadByteB();
                    extraData[Constants.ConstantPoolExtraPropertyName.REFERENCE_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.FIELD_REF/METHOD_REF/INTERFACE_METHOD_REF
                    return extraData;
                case JVMConstantPoolTag.METHOD_TYPE:
                    extraData[Constants.ConstantPoolExtraPropertyName.DESCRIPTOR_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.UTF8
                    return extraData;
                case JVMConstantPoolTag.DYNAMIC:
                case JVMConstantPoolTag.INVOKE_DYNAMIC:
                    extraData[Constants.ConstantPoolExtraPropertyName.BOOTSTRAP_METHOD_ATTRIBUTE_INDEX] = stream.ReadUInt16(); // -> BootstrapMethods Attribute table element index
                    extraData[Constants.ConstantPoolExtraPropertyName.NAME_AND_TYPE_INDEX] = stream.ReadUInt16(); // -> JVMConstantPoolTag.NAME_AND_TYPE
                    return extraData;
                case JVMConstantPoolTag._DUMMY:
                default:
                    throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
            }
        }

        private static JVMAccessFlag[] GetAccessFlags(Stream stream)
        {
            var accessFlags = stream.ReadUInt16();
            return Enum.GetValues<JVMAccessFlag>()
                .Where(f => ((ushort)f & accessFlags) != 0)
                .ToArray();
        }

        private static ushort GetRawInterface(Stream stream)
        {
            return stream.ReadUInt16();
        }

        private static JVMFieldRaw GetRawField(Stream stream)
        {
            var field = new JVMFieldRaw
            {
                AccessFlags = GetAccessFlags(stream),
                NameIndex = stream.ReadUInt16(),
                DescriptorIndex = stream.ReadUInt16(),
                Attributes = GetArrayFromStream(stream, GetRawAttribute),
            };
            return field;
        }

        private static JVMMethodRaw GetRawMethod(Stream stream)
        {
            var method = new JVMMethodRaw
            {
                AccessFlags = GetAccessFlags(stream),
                NameIndex = stream.ReadUInt16(),
                DescriptorIndex = stream.ReadUInt16(),
                Attributes = GetArrayFromStream(stream, GetRawAttribute),
            };
            return method;
        }

        private static JVMAttributeRaw GetRawAttribute(Stream stream)
        {
            var attribute = new JVMAttributeRaw
            {
                AttributeNameIndex = stream.ReadUInt16(),
                Data = stream.ReadBytes((int)stream.ReadUInt32()),
            };
            return attribute;
        }
        #endregion

        #region Raw value processing
        private static string GetInterface(JVMClassRaw rawClass, ushort interfaceIndex)
        {
            return rawClass.ResolveValuePoolValueByIndex<string>(interfaceIndex);
        }

        private static JVMField GetField(JVMClassRaw rawClass, JVMFieldRaw rawField)
        {
            var field = new JVMField
            {
                AccessFlags = rawField.AccessFlags,
                Name = rawClass.ResolveValuePoolValueByIndex<string>(rawField.NameIndex),
                Descriptor = Descriptors.AJVMFieldDescriptor.ParseFieldDescriptor(rawClass.ResolveValuePoolValueByIndex<string>(rawField.DescriptorIndex)),
                Attributes = rawField.Attributes.Select(a => GetAttribute(rawClass, a)).ToArray(),
            };
            return field;
        }

        private static JVMMethod GetMethod(JVMClassRaw rawClass, JVMMethodRaw rawMethod)
        {
            var method = new JVMMethod
            {
                AccessFlags = rawMethod.AccessFlags,
                Name = rawClass.ResolveValuePoolValueByIndex<string>(rawMethod.NameIndex),
                Descriptor = new Descriptors.JVMMethodDescriptor(rawClass.ResolveValuePoolValueByIndex<string>(rawMethod.DescriptorIndex)),
                Attributes = rawMethod.Attributes.Select(a => GetAttribute(rawClass, a)).ToArray(),
            };
            return method;
        }

        #region Attribute data methods
        private static object ResolveValueFromPool(JVMClassRaw rawClass, ushort poolIndex)
        {
            var pool = rawClass.ResolvePoolByIndex(poolIndex);
            return pool.Tag switch
            {
                JVMConstantPoolTag.UTF8 or JVMConstantPoolTag.INTEGER or JVMConstantPoolTag.FLOAT or JVMConstantPoolTag.LONG or JVMConstantPoolTag.DOUBLE or
                    JVMConstantPoolTag.STRING
                    => rawClass.ResolveValuePoolValueByIndex(poolIndex),
                // JVMConstantPoolTag.CLASS => ,
                // JVMConstantPoolTag.FIELD_REF => ,
                // JVMConstantPoolTag.METHOD_REF => ,
                // JVMConstantPoolTag.INTERFACE_METHOD_REF => ,
                // JVMConstantPoolTag.NAME_AND_TYPE => ,
                // JVMConstantPoolTag.METHOD_HANDLE => ,
                // JVMConstantPoolTag.METHOD_TYPE => ,
                // JVMConstantPoolTag.DYNAMIC => ,
                // JVMConstantPoolTag.INVOKE_DYNAMIC => ,
                // JVMConstantPoolTag.MODULE => ,
                // JVMConstantPoolTag.PACKAGE => ,
                _ => throw new ArgumentOutOfRangeException(nameof(pool.Tag), pool.Tag, null),
            };
        }
        
        private static JVMInstruction ParseInstruction(JVMClassRaw rawClass, Stream codeStream)
        {
            var offset = codeStream.Position;
            var opcode = (JVMOpcode)codeStream.ReadByteB();

            var arguments = opcode switch
            {
                JVMOpcode.NOP or JVMOpcode.ACONST_NULL or JVMOpcode.ICONST_NEG_1 or JVMOpcode.ICONST_0 or JVMOpcode.ICONST_1 or JVMOpcode.ICONST_2 or JVMOpcode.ICONST_3 or
                    JVMOpcode.ICONST_4 or JVMOpcode.ICONST_5 or JVMOpcode.LCONST_0 or JVMOpcode.LCONST_1 or JVMOpcode.FCONST_0 or JVMOpcode.FCONST_1 or JVMOpcode.FCONST_2 or
                    JVMOpcode.DCONST_0 or JVMOpcode.DCONST_1 or JVMOpcode.DLOAD_0 or JVMOpcode.DLOAD_1 or JVMOpcode.DLOAD_2 or JVMOpcode.DLOAD_3 or JVMOpcode.ALOAD_0 or
                    JVMOpcode.ALOAD_1 or JVMOpcode.ALOAD_2 or JVMOpcode.ALOAD_3 or JVMOpcode.ASTORE_0 or JVMOpcode.ASTORE_1 or JVMOpcode.ASTORE_2 or JVMOpcode.ASTORE_3 or
                    JVMOpcode.DASTORE or JVMOpcode.DUP or JVMOpcode.ARETURN or JVMOpcode.RETURN or JVMOpcode.ATHROW
                        => Array.Empty<object>(),
                JVMOpcode.INVOKE_SPECIAL or JVMOpcode.INVOKE_VIRTUAL => [rawClass.ResolvePoolByIndex<JVMRefConstantPool>(codeStream.ReadUInt16())],
                JVMOpcode.NEW_ARRAY => [(JVMArrayType)codeStream.ReadByteB()],
                JVMOpcode.LOAD_CONST_WIDE => [rawClass.ResolveValuePoolValueByIndex(codeStream.ReadUInt16())],
                JVMOpcode.PUT_FIELD or JVMOpcode.GET_STATIC => [rawClass.ResolvePoolByIndex<JVMRefConstantPool>(codeStream.ReadUInt16())],
                JVMOpcode.LDC => [ResolveValueFromPool(rawClass, codeStream.ReadByteB())],
                JVMOpcode.LDC_W => [ResolveValueFromPool(rawClass, codeStream.ReadUInt16())],
                JVMOpcode.NEW or JVMOpcode.ANEW_ARRAY => [rawClass.ResolveValuePoolValueByIndex<string>(codeStream.ReadUInt16())],
                JVMOpcode.BIPUSH => [codeStream.ReadByteB()],
                JVMOpcode.SIPUSH => [codeStream.ReadUInt16()],
                _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, null),
            };

            return new JVMInstruction
            {
                Opcode = opcode,
                OriginalOffset = (uint)offset,
                Arguments = arguments.ToArray(),
            };
        }
        
        private static JVMCode GetCode(JVMClassRaw rawClass, byte[] code)
        {
            var codeStream = new MemoryStream(code);
            var instructions = new List<JVMInstruction>();
            while (codeStream.Length != codeStream.Position)
            {
                instructions.Add(ParseInstruction(rawClass, codeStream));
            }
            
            return new JVMCode
            {
                Instructions = instructions.ToArray(),
                OriginalBytes = code,
            };
        }

        private static JVMExceptionTable GetExceptionTable(JVMClassRaw rawClass, Stream codeStream)
        {
            var exceptionTable = new JVMExceptionTable
            {
                StartPC = codeStream.ReadUInt16(),
                EndPC = codeStream.ReadUInt16(),
                HandlerPC = codeStream.ReadUInt16(),
            };
            var catchTypeIndex = codeStream.ReadUInt16();
            exceptionTable.CatchTypeName = catchTypeIndex != 0
                ? rawClass.ResolveValuePoolValueByIndex<string>(catchTypeIndex)
                : null;
            return exceptionTable;
        }

        private static JVMLineNumberTable GetLineNumberTable(Stream tableStream)
        {
            var lineNumberTable = new JVMLineNumberTable
            {
                StartPC = tableStream.ReadUInt16(),
                LineNumber = tableStream.ReadUInt16(),
            };
            return lineNumberTable;
        }

        private static JVMStackMapFrameType GetStackFrameType(byte stackFrameTypeNumber)
        {
            return stackFrameTypeNumber switch
            {
                <= 63 => JVMStackMapFrameType.SAME_FRAME,
                <= 127 => JVMStackMapFrameType.SAME_LOCALS_1_STACK_ITEM_FRAME,
                247 => JVMStackMapFrameType.SAME_LOCALS_1_STACK_ITEM_FRAME_EXTENDED,
                >= 248 and <= 250 => JVMStackMapFrameType.CHOP_FRAME,
                251 => JVMStackMapFrameType.SAME_FRAME_EXTENDED,
                >= 252 and <= 254 => JVMStackMapFrameType.APPEND_FRAME,
                255 => JVMStackMapFrameType.FULL_FRAME,
                _ => throw new ArgumentOutOfRangeException(nameof(stackFrameTypeNumber), stackFrameTypeNumber, null)
            };
        }

        private static JVMVerificationTypeInfo GetVerificationTypeInfo(JVMClassRaw rawClass, Stream stream)
        {
            var verificationType = (JVMVerificationType)stream.ReadByteB();
            return verificationType switch
            {
                JVMVerificationType.TOP or JVMVerificationType.INT or JVMVerificationType.FLOAT
                    or JVMVerificationType.DOUBLE or JVMVerificationType.LONG or JVMVerificationType.NULL
                    or JVMVerificationType.UNINITIALIZED_THIS => new JVMVerificationTypeInfo(verificationType),
                JVMVerificationType.OBJECT => new JVMObjectVerificationTypeInfo(rawClass.ResolveValuePoolValueByIndex<string>(stream.ReadUInt16())),
                JVMVerificationType.UNINITIALIZED => new JVMUninitializedVerificationTypeInfo(stream.ReadUInt16()),
                _ => throw new ArgumentOutOfRangeException(nameof(verificationType), verificationType, null),
            };
        }

        private static JVMStackMapFrame GetStackMapFrame(JVMClassRaw rawClass, Stream tableStream)
        {
            var frameTypeNum = tableStream.ReadByteB();
            var frameType = GetStackFrameType(frameTypeNum);

            return frameType switch
            {
                JVMStackMapFrameType.SAME_FRAME => new JVMStackMapFrame(frameType, frameTypeNum),
                JVMStackMapFrameType.SAME_LOCALS_1_STACK_ITEM_FRAME => new JVMStackMapFrameWithVerification(
                        frameType,
                        frameTypeNum,
                        [GetVerificationTypeInfo(rawClass, tableStream)]
                    ),
                // JVMStackMapFrameType.SAME_LOCALS_1_STACK_ITEM_FRAME_EXTENDED => null,
                // JVMStackMapFrameType.CHOP_FRAME => null,
                // JVMStackMapFrameType.SAME_FRAME_EXTENDED => null,
                // JVMStackMapFrameType.APPEND_FRAME => null,
                // JVMStackMapFrameType.FULL_FRAME => null,
                _ => throw new ArgumentOutOfRangeException(nameof(frameType), frameType, null)
            };
        }
        #endregion

        private static object ProcessAttributeData(JVMClassRaw rawClass, string attributeName, byte[] data)
        {
            switch (attributeName)
            {
                case Constants.AttributeName.SOURCE_FILE:
                    return rawClass.ResolveValuePoolValueByIndex<string>(BinaryPrimitives.ReadUInt16BigEndian(data));
                case Constants.AttributeName.CONSTANT_VALUE:
                    var poolIndex = BinaryPrimitives.ReadUInt16BigEndian(data);
                    var constantPool = rawClass.ResolvePoolByIndex(poolIndex);
                    return constantPool is JVMValueConstantPool valuePool
                        ? valuePool.Value
                        : rawClass.ResolveValuePoolValueByIndex<string>(poolIndex);
                case Constants.AttributeName.CODE:
                    var codeStream = new MemoryStream(data);
                    var codeAttribute = new JVMCodeAttribute
                    {
                        MaxStack = codeStream.ReadUInt16(),
                        MaxLocals = codeStream.ReadUInt16(),
                        Code = GetCode(rawClass, codeStream.ReadBytes((int)codeStream.ReadUInt32())),
                        ExceptionTables = GetArrayFromStream(codeStream, stream => GetExceptionTable(rawClass, stream)),
                        Attributes = GetArrayFromStream(codeStream, GetRawAttribute)
                            .Select(a => GetAttribute(rawClass, a))
                            .ToArray(),
                    };

                    return codeStream.Position == codeStream.Length
                        ? codeAttribute
                        : throw new EndOfStreamException();
                case Constants.AttributeName.LINE_NUMBER_TABLE:
                    return GetArrayFromBytes(data, GetLineNumberTable);
                case Constants.AttributeName.STACK_MAP_TABLE:
                    return GetArrayFromBytes(data, stream => GetStackMapFrame(rawClass, stream));
                default:
                    return BitConverter.ToString(data);
            }
        }

        private static JVMAttribute GetAttribute(JVMClassRaw rawClass, JVMAttributeRaw rawAttribute)
        {
            var attributeName = rawClass.ResolveValuePoolValueByIndex<string>(rawAttribute.AttributeNameIndex);
            var attribute = new JVMAttribute
            {
                Name = attributeName,
                Data = ProcessAttributeData(rawClass, attributeName, rawAttribute.Data),
            };
            return attribute;
        }
        #endregion
        #endregion
    }
}