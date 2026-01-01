using JVMParser.Extensions;
using JVMParser.JVMClasses;
using JVMParser.Mock;

namespace JVMParser
{
    public static class JVMInterpreter
    {
        #region Public methods
        public static void ExecuteMain(JVMClass jvmClass, JVMClass[] references)
        {
            var mainMethod = jvmClass.Methods.First(IsMainMethod);
            var allClasses = references.Append(jvmClass).ToArray();
            var isReturned = ExecuteMethod(allClasses, mainMethod, [], out var returnValue);
        }
        #endregion

        #region Private methods
        private static bool IsMainMethod(JVMMethod method)
        {
            return method is
                {
                    Name: Constants.MAIN_METHOD_NAME,
                    Descriptor.DescriptorString: Constants.MAIN_DESCRIPTOR_STRING,
                    AccessFlags.Length: 2,
                } &&
                method.AccessFlags.Contains(JVMAccessFlag.PUBLIC) &&
                method.AccessFlags.Contains(JVMAccessFlag.STATIC);
        }
        
        private static bool ExecuteMethod(JVMClass[] references, JVMMethod method, object?[] args, out object? returnValue)
        {
            var externalMethodAttr = method.Attributes.FirstOrDefault(a => a.Name == Constants.AttributeName._EXTERNAL_METHOD_MARKER);
            if (externalMethodAttr is not null)
            {
                var externalMethod = (JVMExternalMethod)externalMethodAttr.Data;
                return externalMethod(references, args, out returnValue);
            }
            
            var code = (JVMCodeAttribute)method.Attributes.First(a => a.Name == Constants.AttributeName.CODE).Data;
            var instructions = code.Code.Instructions;

            var locals = new object?[code.MaxLocals];
            var wideLocals = 0;
            for (var x = 0; x < args.Length; x++)
            {
                var arg = args[x];
                locals[x + wideLocals] = arg;
                if (arg is long or double)
                {
                    wideLocals++;
                }
            }
            var stackFrame = new Stack<object?>();
            
            foreach (var instruction in instructions)
            {
                if (ExecuteInstruction(references, stackFrame, locals, instruction, out var retValue))
                {
                    returnValue = retValue.value;
                    return retValue.isValue;
                }
            }
            returnValue = null;
            return false;
        }

        private static JVMClass ResolveClass(JVMClass[] references, string className)
        {
            return references.First(c => c.ThisClass == className);
        }

        private static JVMBootstrapMethod ResolveBootstrapMethod(JVMClass jvmClass, ushort bootstrapMethodIndex)
        {
            return ((JVMBootstrapMethod[])jvmClass.Attributes.First(a => a.Name == Constants.AttributeName.BOOTSTRAP_METHODS).Data)[bootstrapMethodIndex];
        }

        private static JVMField ResolveField(JVMClass[] references, JVMRefConstantPool fieldPool)
        {
            if (fieldPool.Tag != JVMConstantPoolTag.FIELD_REF)
            {
                throw new ArgumentException("Invalid tag", nameof(fieldPool));
            }

            var fieldClass = ResolveClass(references, fieldPool.ClassName);
            return fieldClass.Fields
                .First(f => f.Name == fieldPool.NameAndType.Name && f.Descriptor.DescriptorString == fieldPool.NameAndType.Descriptor.DescriptorString);
        }

        private static JVMMethod ResolveMethod(JVMClass[] references, JVMRefConstantPool methodPool)
        {
            if (
                methodPool.Tag != JVMConstantPoolTag.METHOD_REF &&
                methodPool.Tag != JVMConstantPoolTag.INTERFACE_METHOD_REF
            )
            {
                throw new ArgumentException("Invalid tag", nameof(methodPool));
            }

            var fieldClass = ResolveClass(references, methodPool.ClassName);
            return fieldClass.Methods
                .First(m => m.Name == methodPool.NameAndType.Name && m.Descriptor.DescriptorString == methodPool.NameAndType.Descriptor.DescriptorString);
        }

        private static JVMMethod ResolveMethodHandle(JVMClass[] references, JVMHandleConstantPool handlePool)
        {
            return handlePool.Kind switch
            {
                //JVMReferenceKind.GET_FIELD => expr,
                //JVMReferenceKind.GET_STATIC => expr,
                //JVMReferenceKind.PUT_FIELD => expr,
                //JVMReferenceKind.PUT_STATIC => expr,
                //JVMReferenceKind.INVOKE_VIRTUAL => expr,
                JVMReferenceKind.INVOKE_STATIC => ResolveMethod(references, handlePool.Reference),
                // JVMReferenceKind.INVOKE_SPECIAL => expr,
                // JVMReferenceKind.NEW_INVOKE_SPECIAL => expr,
                // JVMReferenceKind.INVOKE_INTERFACE => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(handlePool.Kind), handlePool.Kind, null),
            };
        }

        private static JVMMethod ResolveDynamicMethod(JVMClass[] references, JVMDynamicConstantPool dynamicPool)
        {
            if (
                dynamicPool.Tag != JVMConstantPoolTag.DYNAMIC &&
                dynamicPool.Tag != JVMConstantPoolTag.INVOKE_DYNAMIC
            )
            {
                throw new ArgumentException("Invalid tag", nameof(dynamicPool));
            }

            var bootstrapClass = ResolveClass(references, dynamicPool.ClassName);
            var bootstrapMethod = ResolveBootstrapMethod(bootstrapClass, dynamicPool.BootstrapMethodAttributeIndex);
            var methodHandle = ResolveMethodHandle(references, bootstrapMethod.BootstrapMethodRef);
            throw new NotImplementedException();
        }

        private static void CallMethod(
            JVMClass[] references,
            Stack<object?> stackFrame,
            JVMRefConstantPool methodPool
        )
        {
            var method = ResolveMethod(references, methodPool);
            var paramCount = method.Descriptor.Parameters.Length;
            var args = Enumerable.Repeat<object?>(null, paramCount)
                .Select(_ => stackFrame.Pop())
                .Reverse()
                .ToArray();
            
            if (ExecuteMethod(references, method, args, out var retV))
            {
                stackFrame.Push(retV);
            }
        }

        private static void CallDynamicMethod(
            JVMClass[] references,
            Stack<object?> stackFrame,
            JVMDynamicConstantPool dynamicPool
        )
        {
            var method = ResolveDynamicMethod(references, dynamicPool);
            var paramCount = method.Descriptor.Parameters.Length;
            var args = Enumerable.Repeat<object?>(null, paramCount)
                .Select(_ => stackFrame.Pop())
                .Reverse()
                .ToArray();
            
            if (ExecuteMethod(references, method, args, out var retV))
            {
                stackFrame.Push(retV);
            }
        }
        
        private static bool ExecuteInstruction(
            JVMClass[] references,
            Stack<object?> stackFrame,
            object?[] locals,
            JVMInstruction instruction,
            out (bool isValue, object? value) returnValue
        )
        {
            returnValue = (false, null);
            switch (instruction.Opcode)
            {
                case JVMOpcode.NOP:
                    break;
                case JVMOpcode.ACONST_NULL:
                    stackFrame.Push(null);
                    break;
                case JVMOpcode.ICONST_NEG_1:
                    stackFrame.Push(-1);
                    break;
                case JVMOpcode.ICONST_0:
                    stackFrame.Push(0);
                    break;
                case JVMOpcode.ICONST_1:
                    stackFrame.Push(1);
                    break;
                case JVMOpcode.ICONST_2:
                    stackFrame.Push(2);
                    break;
                case JVMOpcode.ICONST_3:
                    stackFrame.Push(3);
                    break;
                case JVMOpcode.ICONST_4:
                    stackFrame.Push(4);
                    break;
                case JVMOpcode.ICONST_5:
                    stackFrame.Push(5);
                    break;
                case JVMOpcode.LCONST_0:
                    stackFrame.Push(0L);
                    break;
                case JVMOpcode.LCONST_1:
                    stackFrame.Push(1L);
                    break;
                case JVMOpcode.FCONST_0:
                    stackFrame.Push(0f);
                    break;
                case JVMOpcode.FCONST_1:
                    stackFrame.Push(1f);
                    break;
                case JVMOpcode.FCONST_2:
                    stackFrame.Push(2f);
                    break;
                case JVMOpcode.DCONST_0:
                    stackFrame.Push(0d);
                    break;
                case JVMOpcode.DCONST_1:
                    stackFrame.Push(1d);
                    break;
                case JVMOpcode.BIPUSH:
                case JVMOpcode.SIPUSH:
                    stackFrame.Push((int)instruction.Arguments[0]);
                    break;
                case JVMOpcode.LDC:
                case JVMOpcode.LDC_W:
                    stackFrame.Push(instruction.Arguments[0]); // int/float/reference
                    break;
                case JVMOpcode.LOAD_CONST_WIDE:
                    stackFrame.Push(instruction.Arguments[0]); // long/double
                    break;
                case JVMOpcode.LLOAD_0:
                    stackFrame.Push((long)locals[0]!);
                    break;
                case JVMOpcode.LLOAD_1:
                    stackFrame.Push((long)locals[1]!);
                    break;
                case JVMOpcode.LLOAD_2:
                    stackFrame.Push((long)locals[2]!);
                    break;
                case JVMOpcode.LLOAD_3:
                    stackFrame.Push((long)locals[3]!);
                    break;
                case JVMOpcode.DLOAD_0:
                    stackFrame.Push((double)locals[0]!);
                    break;
                case JVMOpcode.DLOAD_1:
                    stackFrame.Push((double)locals[1]!);
                    break;
                case JVMOpcode.DLOAD_2:
                    stackFrame.Push((double)locals[2]!);
                    break;
                case JVMOpcode.DLOAD_3:
                    stackFrame.Push((double)locals[3]!);
                    break;
                case JVMOpcode.ALOAD_0:
                    stackFrame.Push(locals[0]);
                    break;
                case JVMOpcode.ALOAD_1:
                    stackFrame.Push(locals[1]);
                    break;
                case JVMOpcode.ALOAD_2:
                    stackFrame.Push(locals[2]);
                    break;
                case JVMOpcode.ALOAD_3:
                    stackFrame.Push(locals[3]);
                    break;
                case JVMOpcode.LSTORE_0:
                    locals[0] = stackFrame.Pop<long>();
                    break;
                case JVMOpcode.LSTORE_1:
                    locals[1] = stackFrame.Pop<long>();
                    break;
                case JVMOpcode.LSTORE_2:
                    locals[2] = stackFrame.Pop<long>();
                    break;
                case JVMOpcode.LSTORE_3:
                    locals[3] = stackFrame.Pop<long>();
                    break;
                case JVMOpcode.ASTORE_0:
                    locals[0] = stackFrame.Pop();
                    break;
                case JVMOpcode.ASTORE_1:
                    locals[1] = stackFrame.Pop();
                    break;
                case JVMOpcode.ASTORE_2:
                    locals[2] = stackFrame.Pop();
                    break;
                case JVMOpcode.ASTORE_3:
                    locals[3] = stackFrame.Pop();
                    break;
                case JVMOpcode.DASTORE:
                    var arr = stackFrame.Pop<double[]>();
                    var index = stackFrame.Pop<int>();
                    var value = stackFrame.Pop<double>();
                    arr[index] = value;
                    break;
                case JVMOpcode.DUP:
                    var val = stackFrame.Pop<double[]>();
                    stackFrame.Push(val);
                    stackFrame.Push(val);
                    break;
                case JVMOpcode.POP:
                    stackFrame.Pop();
                    break;
                case JVMOpcode.LADD:
                    stackFrame.Push(stackFrame.Pop<long>() + stackFrame.Pop<long>());
                    break;
                case JVMOpcode.I2D:
                    stackFrame.Push((double)stackFrame.Pop<int>());
                    break;
                case JVMOpcode.ARETURN:
                    var reference = stackFrame.Pop()!;
                    stackFrame.Clear();
                    returnValue = (true, reference);
                    return true;
                case JVMOpcode.RETURN:
                    stackFrame.Clear();
                    return true;
                case JVMOpcode.GET_STATIC:
                    var fieldRefPool = (JVMRefConstantPool)instruction.Arguments[0];
                    var fieldRef = ResolveField(references, fieldRefPool);
                    stackFrame.Push(fieldRef);
                    break;
                // case JVMOpcode.PUT_FIELD:
                //     break;
                case JVMOpcode.INVOKE_VIRTUAL:
                    var methodPoolV = (JVMRefConstantPool)instruction.Arguments[0];
                    CallMethod(references, stackFrame, methodPoolV);
                    break;
                // case JVMOpcode.INVOKE_SPECIAL:
                //     break;
                case JVMOpcode.INVOKE_STATIC:
                    var methodPoolS = (JVMRefConstantPool)instruction.Arguments[0];
                    CallMethod(references, stackFrame, methodPoolS);
                    break;
                case JVMOpcode.INVOKE_DYNAMIC:
                    var methodPoolD = (JVMDynamicConstantPool)instruction.Arguments[0];
                    CallDynamicMethod(references, stackFrame, methodPoolD);
                    break;
                // case JVMOpcode.NEW:
                //     break;
                // case JVMOpcode.NEW_ARRAY:
                //     break;
                // case JVMOpcode.ANEW_ARRAY:
                //     break;
                // case JVMOpcode.ATHROW:
                //     break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instruction.Opcode), instruction.Opcode, null);
            }

            return false;
        }
        #endregion
    }
}