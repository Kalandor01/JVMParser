using System.Diagnostics.CodeAnalysis;
using JVMParser.JVMClasses;

namespace JVMParser
{
    #region Processed value classes
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

    public class JVMBootstrapMethod
    {
        public JVMHandleConstantPool BootstrapMethodRef;
        public AJVMConstantPool[] BootstrapArguments;

        public override string ToString()
        {
            return BootstrapMethodRef.ToString();
        }
    }

    public class JVMInnerClass
    {
        public string InnerClassInfo;
        public string? OuterClassInfo;
        public string? InnerName;
        public JVMAccessFlag[] AccessFlags;

        public override string ToString()
        {
            return $"{(AccessFlags.Length != 0 ? $"{string.Join(" ", AccessFlags).ToLower()} " : "")} {InnerClassInfo}";
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
        public Descriptors.AJVMFieldDescriptor Descriptor;
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
        public Descriptors.JVMMethodDescriptor Descriptor;
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