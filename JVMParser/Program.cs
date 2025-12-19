namespace JVMParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var testFolderPath = Path.GetFullPath("../../../../TestFiles");
            var javaClassExtension = ".class";
            
            var testFile = "Test";
            var abstractTestFile = "ATest";
            var interfaceTestFile = "ITest";
            
            var testFilePath = Path.Join(testFolderPath, testFile + javaClassExtension);
            var jvmClassRaw = JVMParser.Parse(testFilePath);

            JVMClass jvmClass;
            if (jvmClassRaw is not null)
            {
                jvmClass = JVMParser.RevolveJVMClass(jvmClassRaw);
            }
        }
    }
}