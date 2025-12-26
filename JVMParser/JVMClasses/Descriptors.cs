namespace JVMParser.JVMClasses;

public class Descriptors
{
    public interface IJVMDescriptor
    {
        string ToDescriptorString();
        string ToString(string name);
        
        public static IJVMDescriptor ParseDescriptor(string descriptorString)
        {
            if (descriptorString.StartsWith('('))
            {
                return new JVMMethodDescriptor(descriptorString);
            }
            
            var fieldDescriptor = AJVMFieldDescriptor.ParseFieldDescriptor(ref descriptorString);
            return descriptorString.Length == 0
                ? fieldDescriptor
                : throw new ArgumentException($"Remaining descriptor data after parsing: \"{descriptorString}\"");
        }
    }
    
    public abstract class AJVMFieldDescriptor : IJVMDescriptor
    {
        public static AJVMFieldDescriptor ParseFieldDescriptor(ref string descriptorString)
        {
            if (descriptorString.Length < 1)
            {
                throw new ArgumentException("Descriptor is empty!");
            }

            var firstChar = descriptorString[0];
            descriptorString = descriptorString[1..];
            switch (firstChar)
            {
                case 'B':
                    return new JVMFieldDescriptor(JVMFieldType.BYTE);
                case 'C':
                    return new JVMFieldDescriptor(JVMFieldType.CHAR);
                case 'D':
                    return new JVMFieldDescriptor(JVMFieldType.DOUBLE);
                case 'F':
                    return new JVMFieldDescriptor(JVMFieldType.FLOAT);
                case 'I':
                    return new JVMFieldDescriptor(JVMFieldType.INT);
                case 'J':
                    return new JVMFieldDescriptor(JVMFieldType.LONG);
                case 'S':
                    return new JVMFieldDescriptor(JVMFieldType.SHORT);
                case 'Z':
                    return new JVMFieldDescriptor(JVMFieldType.BOOL);
                case '[':
                    return new JVMArrayDescriptor(ParseFieldDescriptor(ref descriptorString));
                case 'L':
                    var classEndIndex = descriptorString.IndexOf(';');
                    if (classEndIndex == -1)
                    {
                        throw new ArgumentException("No class field end character!");
                    }

                    var className = descriptorString[..classEndIndex];
                    descriptorString = descriptorString[(classEndIndex + 1)..];
                    return new JVMClassFieldDescriptor(className);
                default:
                    throw new ArgumentException("Unknown descriptor type!");
            }
        }

        public static AJVMFieldDescriptor ParseFieldDescriptor(string descriptorString)
        {
            var res = ParseFieldDescriptor(ref descriptorString);
            return descriptorString.Length == 0
                ? res
                : throw new ArgumentException($"Remaining descriptor data after parsing: \"{descriptorString}\"");
        }

        public abstract string ToDescriptorString();
        public abstract string ToString(string name);
    }
    
    public class JVMFieldDescriptor : AJVMFieldDescriptor
    {
        public readonly JVMFieldType FieldType;
        
        public JVMFieldDescriptor(JVMFieldType fieldType)
        {
            FieldType = fieldType;
        }

        public override string ToDescriptorString()
        {
            return FieldType switch
            {
                JVMFieldType.BYTE => "B",
                JVMFieldType.CHAR => "C",
                JVMFieldType.DOUBLE => "D",
                JVMFieldType.FLOAT => "F",
                JVMFieldType.INT => "I",
                JVMFieldType.LONG => "J",
                JVMFieldType.SHORT => "S",
                JVMFieldType.BOOL => "Z",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override string ToString(string fieldName)
        {
            return $"{FieldType.ToString().ToLower()} {fieldName}";
        }

        public override string? ToString()
        {
            return FieldType.ToString().ToLower();
        }
    }
    
    public class JVMClassFieldDescriptor : AJVMFieldDescriptor
    {
        public readonly string ClassName;
        
        public JVMClassFieldDescriptor(string className)
        {
            ClassName = className;
        }

        public override string ToDescriptorString()
        {
            return $"L{ClassName};";
        }

        public override string ToString(string fieldName)
        {
            return $"{ClassName} {fieldName}";
        }

        public override string? ToString()
        {
            return ClassName;
        }
    }
    
    public class JVMArrayDescriptor : AJVMFieldDescriptor
    {
        public readonly AJVMFieldDescriptor Field;
        
        public JVMArrayDescriptor(AJVMFieldDescriptor field)
        {
            Field = field;
        }

        public override string ToDescriptorString()
        {
            return '[' + Field.ToDescriptorString();
        }

        public override string ToString(string fieldName)
        {
            return $"{Field}[] {fieldName}";
        }

        public override string? ToString()
        {
            return $"{Field}[]";
        }
    }
    
    public class JVMMethodDescriptor : IJVMDescriptor
    {
        public readonly AJVMFieldDescriptor[] Parameters;
        public readonly AJVMFieldDescriptor? ReturnType;
        
        public JVMMethodDescriptor(string descriptorString)
        {
            if (!descriptorString.StartsWith('('))
            {
                throw new ArgumentException("Invalid method descriptor start!");
            }

            var paramsAndReturn = descriptorString.TrimStart('(').Split(')');
            if (paramsAndReturn.Length != 2)
            {
                throw new ArgumentException("No parameters and return part found in method descriptor!");
            }

            var returnDescriptor = paramsAndReturn[1];
            ReturnType = returnDescriptor != "V"
                ? AJVMFieldDescriptor.ParseFieldDescriptor(returnDescriptor)
                : null;
            
            var paramDescriptors = paramsAndReturn[0];
            var parameters = new List<AJVMFieldDescriptor>();
            while (paramDescriptors.Length != 0)
            {
                parameters.Add(AJVMFieldDescriptor.ParseFieldDescriptor(ref paramDescriptors));
            }
            Parameters = parameters.ToArray();
        }

        public string ToDescriptorString()
        {
            var parameters = string.Join("", Parameters.Select(p => p.ToDescriptorString()));
            return $"({parameters}){(ReturnType is not null ? ReturnType.ToDescriptorString() : "V")}";
        }

        public string ToString(string methodName = "Method")
        {
            return $"{(ReturnType is not null ? ReturnType : "void")} {methodName}({string.Join(", ", Parameters)});";
        }

        public override string? ToString()
        {
            return ToString();
        }
    }
}