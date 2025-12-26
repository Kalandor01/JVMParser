using JVMParser.Extensions;
using JVMParser.JVMClasses;

namespace JVMParser
{
    public static class JVMInterpreter
    {
        #region Public methods
        public static void ExecuteMain(JVMClass jvmClass, JVMClass[] references)
        {
            var mainMethod = jvmClass.Methods.First(IsMainMethod);
            var allClasses = references.Append(jvmClass).ToArray();
            ExecuteMethod(mainMethod, allClasses);
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
        
        private static void ExecuteMethod(JVMMethod method, JVMClass[] references)
        {
            var code = (JVMCodeAttribute)method.Attributes.First(a => a.Name == Constants.AttributeName.CODE).Data;
            var instructions = code.Code.Instructions;

            var locals = new object?[code.MaxLocals];
            var stackFrame = new Stack<object?>();
            var isReturn = false;
            object? retValue = null;
            foreach (var instruction in instructions)
            {
                isReturn = ExecuteInstruction(references, stackFrame, locals, instruction, out retValue);
            }
        }
        
        private static bool ExecuteInstruction(JVMClass[] references, Stack<object?> stackFrame, object?[] locals, JVMInstruction instruction, out object? returnValue)
        {
            returnValue = null;
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
                    stackFrame.Push((byte)instruction.Arguments[0]);
                    break;
                case JVMOpcode.SIPUSH:
                    stackFrame.Push((ushort)instruction.Arguments[0]);
                    break;
                case JVMOpcode.LDC:
                case JVMOpcode.LDC_W:
                    stackFrame.Push(instruction.Arguments[0]);
                    break;
                case JVMOpcode.LOAD_CONST_WIDE:
                    stackFrame.Push(instruction.Arguments[0]);
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
                    var arr = (double[])stackFrame.Pop()!;
                    var index = (int)stackFrame.Pop()!;
                    var value = (double)stackFrame.Pop()!;
                    arr[index] = value;
                    break;
                case JVMOpcode.DUP:
                    var val = (double[])stackFrame.Pop()!;
                    stackFrame.Push(val);
                    stackFrame.Push(val);
                    break;
                case JVMOpcode.ARETURN:
                    var reference = stackFrame.Pop()!;
                    stackFrame.Clear();
                    returnValue = reference;
                    return true;
                case JVMOpcode.RETURN:
                    stackFrame.Clear();
                    return true;
                case JVMOpcode.GET_STATIC:
                    var fieldRef = (JVMRefConstantPool)instruction.Arguments[0];
                    
                    stackFrame.Push(fieldRef);
                    break;
                // case JVMOpcode.PUT_FIELD:
                //     break;
                // case JVMOpcode.INVOKE_VIRTUAL:
                //     break;
                // case JVMOpcode.INVOKE_SPECIAL:
                //     break;
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