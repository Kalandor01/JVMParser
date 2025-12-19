namespace JVMParser;

internal class Constants
{
    public class ConstantPoolExtraPropertyName
    {
        public const string name_index = "name_index";
        public const string string_index = "string_index";
        public const string class_index = "class_index";
        public const string name_and_type_index = "name_and_type_index";
        public const string descriptor_index = "descriptor_index";
        public const string reference_kind = "reference_kind";
        public const string reference_index = "reference_index";
        public const string bootstrap_method_attr_index = "bootstrap_method_attr_index";
    }
    
    public class AttributeName
    {
        public const string ConstantValue = "ConstantValue";
        public const string Code = "Code";
        public const string StackMapTable = "StackMapTable";
        public const string Exceptions = "Exceptions";
        public const string InnerClasses = "InnerClasses";
        public const string EnclosingMethod = "EnclosingMethod";
        public const string Synthetic = "Synthetic";
        public const string Signature = "Signature";
        public const string SourceFile = "SourceFile";
        public const string SourceDebugExtension = "SourceDebugExtension";
        public const string LineNumberTable = "LineNumberTable";
        public const string LocalVariableTable = "LocalVariableTable";
        public const string LocalVariableTypeTable = "LocalVariableTypeTable";
        public const string Deprecated = "Deprecated";
        public const string RuntimeVisibleAnnotations = "RuntimeVisibleAnnotations";
        public const string RuntimeInvisibleAnnotations = "RuntimeInvisibleAnnotations";
        public const string RuntimeVisibleParameterAnnotations = "RuntimeVisibleParameterAnnotations";
        public const string RuntimeInvisibleParameterAnnotations = "RuntimeInvisibleParameterAnnotations";
        public const string RuntimeVisibleTypeAnnotations = "RuntimeVisibleTypeAnnotations";
        public const string RuntimeInvisibleTypeAnnotations = "RuntimeInvisibleTypeAnnotations";
        public const string AnnotationDefault = "AnnotationDefault";
        public const string BootstrapMethods = "BootstrapMethods";
        public const string MethodParameters = "MethodParameters";
        public const string Module = "Module";
        public const string ModulePackages = "ModulePackages";
        public const string ModuleMainClass = "ModuleMainClass";
        public const string NestHost = "NestHost";
        public const string NestMembers = "NestMembers";
        public const string Record = "Record";
        public const string PermittedSubclasses = "PermittedSubclasses";
    }
}