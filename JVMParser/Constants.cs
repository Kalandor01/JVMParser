namespace JVMParser;

internal class Constants
{
    public const string INIT_METHOD_NAME = "<init>";
    public const string MAIN_METHOD_NAME = "main";
    public const string MAIN_DESCRIPTOR_STRING = "([Ljava/lang/String;)V";
    
    public class ConstantPoolExtraPropertyName
    {
        public const string VALUE = "value";
        public const string NAME_INDEX = "name_index";
        public const string STRING_INDEX = "string_index";
        public const string CLASS_INDEX = "class_index";
        public const string NAME_AND_TYPE_INDEX = "name_and_type_index";
        public const string DESCRIPTOR_INDEX = "descriptor_index";
        public const string REFERENCE_KIND = "reference_kind";
        public const string REFERENCE_INDEX = "reference_index";
        public const string BOOTSTRAP_METHOD_ATTRIBUTE_INDEX = "bootstrap_method_attr_index";
    }
    
    public class AttributeName
    {
        public const string CONSTANT_VALUE = "ConstantValue";
        public const string CODE = "Code";
        public const string STACK_MAP_TABLE = "StackMapTable";
        public const string EXCEPTIONS = "Exceptions";
        public const string INNER_CLASSES = "InnerClasses";
        public const string EnclosingMethod = "EnclosingMethod";
        public const string Synthetic = "Synthetic";
        public const string Signature = "Signature";
        public const string SOURCE_FILE = "SourceFile";
        public const string SourceDebugExtension = "SourceDebugExtension";
        public const string LINE_NUMBER_TABLE = "LineNumberTable";
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