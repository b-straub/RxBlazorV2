using GeneratorTest.Helpers;

namespace GeneratorRunner
{
    class Program
    {
        private static readonly string[] SourcePaths =
        [
            "RxBlazorV2Sample/Program.cs",
            "RxBlazorV2Sample/Model/CounterModel.cs",
        ];
        
        private static readonly string[] AdditionalTextPaths =
        [
            "RxBlazorV2Sample/Pages/Counter2.razor"
        ];

        public static void Main(string[] _)
        {
            List<string> sources = new();
            List<(string Text, string Path)> additionalText = new();

            foreach (var path in SourcePaths)
            {
                var fullPath = Path.Combine(GetProjectRootFromBase(), "..", path);
                
                if (File.Exists(fullPath))
                {
                    sources.Add(File.ReadAllText(fullPath));
                    Console.WriteLine($"Loaded source: {fullPath}");
                }
                else
                {
                    throw new InvalidOperationException($"Error: File not found: {fullPath}");
                }
            }

            foreach (var path in AdditionalTextPaths)
            {
                var fullPath = Path.Combine(GetProjectRootFromBase(), "..", path);
                if (File.Exists(fullPath))
                {
                    additionalText.Add((File.ReadAllText(fullPath), fullPath));
                    Console.WriteLine($"Loaded additional text: {fullPath}");
                }
                else
                {
                    throw new InvalidOperationException($"Error: Additional text file not found: {fullPath}");
                }
            }
            
            var diags = GeneratorFactory.RunGenerator(sources, additionalText);

            foreach (var diag in diags)
            {
                Console.WriteLine($"Message: {diag.GetMessage()}, Location: {diag.Location.GetLineSpan()}");
            }
        }
        
        private static string GetProjectRootFromBase()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var current = new DirectoryInfo(baseDirectory);

            // Navigate up to find the project root (where .csproj file is)
            while (current != null && !current.GetFiles("*.csproj").Any())
            {
                current = current.Parent;
            }

            return current?.FullName ?? baseDirectory;
        }
    }
}