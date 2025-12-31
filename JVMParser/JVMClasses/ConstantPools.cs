namespace JVMParser.JVMClasses
{
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
        public readonly Descriptors.IJVMDescriptor Descriptor;
    
        public JVMNameTypeConstantPool(JVMConstantPoolTag tag, string name, string descriptor)
            : base(tag)
        {
            Name = name;
            Descriptor = Descriptors.IJVMDescriptor.ParseDescriptor(descriptor);
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
        public string ClassName { get; private set; }
        public readonly ushort BootstrapMethodAttributeIndex;
        public readonly JVMNameTypeConstantPool NameAndType;
    
        public JVMDynamicConstantPool(JVMConstantPoolTag tag, ushort bootstrapIndex, JVMNameTypeConstantPool nameAndType)
            : base(tag)
        {
            ClassName = null;
            BootstrapMethodAttributeIndex = bootstrapIndex;
            NameAndType = nameAndType;
        }

        public object SetClassName(string className)
        {
            ClassName = className;
            return this;
        }
    
        public override string? ToString()
        {
            return $"{Tag}: ({BootstrapMethodAttributeIndex}), {NameAndType.ToString(false)}";
        }
    }
}