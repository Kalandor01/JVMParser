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

            var parsedClasses = testFileNames
                .Select(testFileName => (testFileName, ParseJVMBytecode(testFileName)))
                .ToList();
        }

        private static JVMClass? ParseJVMBytecode(string fileName)
        {
            var testFolderPath = Path.GetFullPath("../../../../TestFiles");
            const string javaClassExtension = ".class";
            
            var testFilePath = Path.Join(testFolderPath, fileName + javaClassExtension);
            var jvmClassRaw = JVMParser.Parse(testFilePath);

            return jvmClassRaw is not null
                ? JVMParser.RevolveJVMClass(jvmClassRaw)
                : null;
        }
    }
}