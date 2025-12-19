using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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

        public override string ToString()
        {
            return $"{Tag}: {{ {string.Join(", ", ExtraData.Select(kv => $"{kv.Key} = {kv.Value}") ?? [])} }}";
        }
    }
    
    public class JVMValueConstantPool<T> : JVMConstantPool
    {
        public T Value;

        public JVMValueConstantPool(JVMConstantPoolTag tag, T value)
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

        public override string ToString()
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
        

        public virtual bool TryResolvePoolByIndex(ushort propertyIndex, [NotNullWhen(true)] out JVMConstantPool? pool)
        {
            if (ConstantPools.Length >= propertyIndex)
            {
                pool = ConstantPools[propertyIndex - 1];
                return true;
            }

            pool = null;
            return false;
        }
        
        public virtual JVMConstantPool ResolvePoolByIndex(ushort propertyIndex)
        {
            return ConstantPools[propertyIndex - 1];
        }
        
        public virtual T ResolveValuePoolValueByIndex<T>(ushort propertyIndex)
        {
            return ((JVMValueConstantPool<T>)ConstantPools[propertyIndex - 1]).Value;
        }

        public string RecursivelyResolveConstantPool(ushort index)
        {
            var resolvedPool = ResolvePoolByIndex(index);
            while (true)
            {
                if (resolvedPool is JVMValueConstantPool<string> valuePool)
                {
                    return valuePool.Value;
                }

                switch (resolvedPool.Tag)
                {
                    case JVMConstantPoolTag.UTF8:
                    case JVMConstantPoolTag.INTEGER:
                        return ((JVMValueConstantPool<int>)resolvedPool).Value.ToString();
                    case JVMConstantPoolTag.FLOAT:
                        return ((JVMValueConstantPool<float>)resolvedPool).Value.ToString(CultureInfo.InvariantCulture);
                    case JVMConstantPoolTag.LONG:
                        return ((JVMValueConstantPool<long>)resolvedPool).Value.ToString();
                    case JVMConstantPoolTag.DOUBLE:
                        return ((JVMValueConstantPool<double>)resolvedPool).Value.ToString(CultureInfo.InvariantCulture);
                    case JVMConstantPoolTag.CLASS:
                    case JVMConstantPoolTag.MODULE:
                    case JVMConstantPoolTag.PACKAGE:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.name_index);
                        continue;
                    case JVMConstantPoolTag.STRING:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.string_index);
                        continue;
                    case JVMConstantPoolTag.METHOD_TYPE:
                        resolvedPool = resolvedPool.ResolvePoolByIndexProperty(this, Constants.ConstantPoolExtraPropertyName.descriptor_index);
                        continue;
                    case JVMConstantPoolTag.FIELD_REF:
                    case JVMConstantPoolTag.METHOD_REF:
                    case JVMConstantPoolTag.INTERFACE_METHOD_REF:
                    case JVMConstantPoolTag.NAME_AND_TYPE:
                    case JVMConstantPoolTag.METHOD_HANDLE:
                    case JVMConstantPoolTag.DYNAMIC:
                    case JVMConstantPoolTag.INVOKE_DYNAMIC:
                    case JVMConstantPoolTag._DUMMY:
                    default:
                        return $"[UNRESOLVABLE] {resolvedPool}";
                }
            }
        }
    }
    #endregion

    #region Processed value classes
    public class JVMMethodDescriptor
    {
        public object NameOrMethodRef;

        public JVMMethodDescriptor(JVMClassRaw rawClass, ushort descriptorIndex)
        {
            var resolvedPool = rawClass.ResolvePoolByIndex(descriptorIndex);
            if (resolvedPool.Tag == JVMConstantPoolTag.METHOD_REF)
            {
                var classNameIndex = (ushort)resolvedPool.ExtraData[Constants.ConstantPoolExtraPropertyName.class_index];
                var nameAndTypeIndex = (ushort)resolvedPool.ExtraData[Constants.ConstantPoolExtraPropertyName.name_and_type_index];
                NameOrMethodRef = (
                    rawClass.RecursivelyResolveConstantPool(classNameIndex),
                    rawClass.RecursivelyResolveConstantPool(nameAndTypeIndex)
                );
            }
            else
            {
                NameOrMethodRef = rawClass.RecursivelyResolveConstantPool(descriptorIndex);
            }
        }
    }
        
    public class JVMField
    {
        public JVMAccessFlag[] AccessFlags;
        public string Name;
        public string Descriptor;
        public JVMAttribute[] Attributes;

        public override string ToString()
        {
            return Name;
        }
    }

    public class JVMMethod
    {
        public JVMAccessFlag[] AccessFlags;
        public string Name;
        public string Descriptor;
        public JVMAttribute[] Attributes;

        public override string ToString()
        {
            return Name;
        }
    }

    public class JVMAttribute
    {
        public string Name;
        public object Data;

        public override string ToString()
        {
            return Name;
        }
    }
    
    public class JVMClass
    {
        public string Magic;
        public ushort MajorVersion;
        public ushort MinorVersion;
        // public JVMConstantPool[] ConstantPools;
        public JVMAccessFlag[] AccessFlags;
        public string ThisClass;
        public string SuperClass;
        public string[] Interfaces;
        public JVMField[] Fields;
        public JVMMethod[] Methods;
        public JVMAttribute[] Attributes;

        public override string ToString()
        {
            return $"{ThisClass} : {SuperClass} ({MajorVersion}.{MinorVersion})";
        }
    }
    #endregion
}