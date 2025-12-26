using System.Text;
using JVMParser.Extensions;
using JVMParser.JVMClasses;

namespace JVMParser;

public class JVMRawParser
{
    #region Public methods
        public static JVMAttributeRaw ParseRawAttribute(Stream stream)
        {
            var attribute = new JVMAttributeRaw
            {
                AttributeNameIndex = stream.ReadUInt16(),
                Data = stream.ReadBytes((int)stream.ReadUInt32()),
            };
            return attribute;
        }
    
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
                Interfaces = stream.ParseArray(GetRawInterface),
                Fields = stream.ParseArray(GetRawField),
                Methods = stream.ParseArray(GetRawMethod),
                Attributes = stream.ParseArray(ParseRawAttribute),
            };

            return stream.Position != stream.Length
                ? throw new EndOfStreamException()
                : jvmClass;
        }
        #endregion
        
        #region Private methods
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
                Attributes = stream.ParseArray(ParseRawAttribute),
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
                Attributes = stream.ParseArray(ParseRawAttribute),
            };
            return method;
        }
        #endregion
}