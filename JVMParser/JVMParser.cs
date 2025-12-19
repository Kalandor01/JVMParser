using System.Text;

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
                // ConstantPools = GetConstantPools(stream),
                AccessFlags = jvmClassRaw.AccessFlags,
                ThisClass = jvmClassRaw.RecursivelyResolveConstantPool(jvmClassRaw.ThisClassIndex),
                SuperClass = jvmClassRaw.RecursivelyResolveConstantPool(jvmClassRaw.SuperClassIndex),
                Interfaces = jvmClassRaw.Interfaces.Select(i => GetInterface(jvmClassRaw, i)).ToArray(),
                Fields = jvmClassRaw.Fields.Select(f => GetField(jvmClassRaw, f)).ToArray(),
                Methods = jvmClassRaw.Methods.Select(m => GetMethod(jvmClassRaw, m)).ToArray(),
                Attributes = jvmClassRaw.Attributes.Select(a => GetAttribute(jvmClassRaw, a)).ToArray(),
            };
            return jvmClass;
        }

        #endregion

        #region Private methods
        #region Stream parsing
        private static T[] GetArrayFromStream<T>(Stream stream, Func<Stream, T> itemProcessor)
        {
            var count = stream.ReadUInt16();
            var list = new List<T>();
            for (var x = 0; x < count; x++)
            {
                var item = itemProcessor(stream);
                list.Add(item);
            }
            return list.ToArray();
        }
        
        private static JVMConstantPool[] GetConstantPools(Stream stream)
        {
            var constantPoolCount = stream.ReadUInt16();
            var constantPools = new List<JVMConstantPool>();
            for (var x = 1; x < constantPoolCount; x++)
            {
                var tag = (JVMConstantPoolTag)stream.ReadByte();
                var constantPool = GetJVMConstantPool(stream, tag, out var addExtra);
                constantPools.Add(constantPool);
                if (addExtra)
                {
                    constantPools.Add(new JVMValueConstantPool<object>(JVMConstantPoolTag._DUMMY, null!));
                    x++;
                }
            }
            return constantPools.ToArray();
        }
        
        private static JVMConstantPool GetJVMConstantPool(Stream stream, JVMConstantPoolTag tag, out bool addExtra)
        {
            addExtra = false;
            var extraData = new Dictionary<string, object>();
            switch (tag)
            {
                case JVMConstantPoolTag.UTF8:
                    var length = stream.ReadUInt16();
                    return new JVMValueConstantPool<string>(tag, Encoding.UTF8.GetString(stream.ReadBytes(length)));
                case JVMConstantPoolTag.INTEGER:
                    return new JVMValueConstantPool<int>(tag, stream.ReadInt32());
                case JVMConstantPoolTag.FLOAT:
                    return new JVMValueConstantPool<float>(tag, stream.ReadFloat());
                case JVMConstantPoolTag.LONG:
                    addExtra = true;
                    return new JVMValueConstantPool<long>(tag, stream.ReadInt64());
                case JVMConstantPoolTag.DOUBLE:
                    addExtra = true;
                    return new JVMValueConstantPool<double>(tag, stream.ReadDouble());
                case JVMConstantPoolTag.CLASS:
                case JVMConstantPoolTag.MODULE:
                case JVMConstantPoolTag.PACKAGE:
                    extraData[Constants.ConstantPoolExtraPropertyName.name_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.STRING:
                    extraData[Constants.ConstantPoolExtraPropertyName.string_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.FIELD_REF:
                case JVMConstantPoolTag.METHOD_REF:
                case JVMConstantPoolTag.INTERFACE_METHOD_REF:
                    extraData[Constants.ConstantPoolExtraPropertyName.class_index] = stream.ReadUInt16();
                    extraData[Constants.ConstantPoolExtraPropertyName.name_and_type_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.NAME_AND_TYPE:
                    extraData[Constants.ConstantPoolExtraPropertyName.name_index] = stream.ReadUInt16();
                    extraData[Constants.ConstantPoolExtraPropertyName.descriptor_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.METHOD_HANDLE:
                    extraData[Constants.ConstantPoolExtraPropertyName.reference_kind] = (JVMReferenceKind)stream.ReadByte();
                    extraData[Constants.ConstantPoolExtraPropertyName.reference_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.METHOD_TYPE:
                    extraData[Constants.ConstantPoolExtraPropertyName.descriptor_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
                case JVMConstantPoolTag.DYNAMIC:
                case JVMConstantPoolTag.INVOKE_DYNAMIC:
                    extraData[Constants.ConstantPoolExtraPropertyName.bootstrap_method_attr_index] = stream.ReadUInt16();
                    extraData[Constants.ConstantPoolExtraPropertyName.name_and_type_index] = stream.ReadUInt16();
                    return new JVMConstantPool(tag, extraData);
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
            return rawClass.RecursivelyResolveConstantPool(interfaceIndex);
        }

        private static JVMField GetField(JVMClassRaw rawClass, JVMFieldRaw rawField)
        {
            var field = new JVMField
            {
                AccessFlags = rawField.AccessFlags,
                Name = rawClass.RecursivelyResolveConstantPool(rawField.NameIndex),
                Descriptor = rawClass.RecursivelyResolveConstantPool(rawField.DescriptorIndex),
                Attributes = rawField.Attributes.Select(a => GetAttribute(rawClass, a)).ToArray(),
            };
            return field;
        }

        private static JVMMethod GetMethod(JVMClassRaw rawClass, JVMMethodRaw rawMethod)
        {
            var method = new JVMMethod
            {
                AccessFlags = rawMethod.AccessFlags,
                Name = rawClass.RecursivelyResolveConstantPool(rawMethod.NameIndex),
                Descriptor = rawClass.RecursivelyResolveConstantPool(rawMethod.DescriptorIndex),
                // Descriptor = new JVMMethodDescriptor(rawClass, rawMethod.DescriptorIndex),
                Attributes = rawMethod.Attributes.Select(a => GetAttribute(rawClass, a)).ToArray(),
            };
            return method;
        }

        private static object ProcessAttributeData(JVMClassRaw rawClass, string attributeName, byte[] data)
        {
            return BitConverter.ToString(data);
        }

        private static JVMAttribute GetAttribute(JVMClassRaw rawClass, JVMAttributeRaw rawAttribute)
        {
            var attributeName = rawClass.RecursivelyResolveConstantPool(rawAttribute.AttributeNameIndex);
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