using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using JVMParser.Extensions;
using JVMParser.JVMClasses;

namespace JVMParser
{
    public class JVMParser
    {
        #region Public methods
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
        
        private static T[] GetArrayFromBytes<T>(byte[] bytes, Func<Stream, T> itemProcessor)
        {
            var stream = new MemoryStream(bytes);
            var array = stream.ParseArray(itemProcessor);
            return stream.Position == stream.Length
                ? array
                : throw new EndOfStreamException();
        }

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
                        ExceptionTables = codeStream.ParseArray(stream => GetExceptionTable(rawClass, stream)),
                        Attributes = codeStream.ParseArray(JVMRawParser.ParseRawAttribute)
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
