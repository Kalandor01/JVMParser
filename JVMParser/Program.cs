namespace JVMParser
{
    class Program
    {
        internal static void Main(string[] args)
        {
            var testFileNames = new List<string>
            {
                "Test",
                "ATest",
                "ITest",
            };
            const string executingClass = "Test";

            var parsedClasses = testFileNames
                .Select(testFileName => (fileName: testFileName, jvmClass: ParseJVMBytecode(testFileName)))
                .ToList();

            var testClass = parsedClasses.First(c => c.fileName == executingClass).jvmClass!;
            var otherClasses = parsedClasses
                .Where(c => c.fileName != executingClass)
                .Select(c => c.jvmClass!)
                .Append(JVMMock.MockSystemClass())
                .Append(JVMMock.MockPrintStreamClass())
                .ToArray();
            
            JVMInterpreter.ExecuteMain(testClass, otherClasses);
        }

        private static JVMClass? ParseJVMBytecode(string fileName)
        {
            var testFolderPath = Path.GetFullPath("../../../../TestFiles");
            const string javaClassExtension = ".class";
            
            var testFilePath = Path.Join(testFolderPath, fileName + javaClassExtension);
            var jvmRawClass = JVMRawParser.Parse(testFilePath);

            return jvmRawClass is not null
                ? JVMParser.RevolveJVMClass(jvmRawClass)
                : null;
        }
    }
}