using System.Diagnostics.CodeAnalysis;

namespace JVMParser.JVMClasses
{
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
                : $"{Tag}: {{ {string.Join(", ", ExtraData.Select(kv => $"{kv.Key} = {kv.Value}"))} }}";
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
}