using VSharp.TestProcessing;

namespace TestCrosscheckerRunner;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("INSUFFICIENT ARGUMENTS GIVEN");
            return -2;
        }
        var isReals = args[0] == "TRUE";

        var dir = Environment.GetEnvironmentVariable(isReals ? "RealTestProject" : "FakeTestProject");
        if (dir is null)
        {
            Console.Error.WriteLine("FAIL TO GET THE DIRECTORY");
            return -1;
        }

        var generated = new DirectoryInfo(@"C:\VSharpProject\VSharp\VSharp.Test\GeneratedTests");
        if (generated.Exists)
            generated.Delete(true);
        
        VSharp.Logger.changeVerbosity("", VSharp.Logger.Info);
        var dirInfo = new DirectoryInfo(dir);
        var api = new TestProcessingAPI(dirInfo, true);
        
        //api.EmptyTestRun();
        
        Console.WriteLine();
        Console.WriteLine("########## MINIMIZED #############");
        Console.WriteLine();

        var minimizeReport = api.MinimizeTests();
        foreach (var name in minimizeReport.minimized)
        {
            if (name != null)
                Console.WriteLine($"{name}\n");
            else
            {
                Console.WriteLine("??null");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("############# UNUSED #############");
        Console.WriteLine();
        
        foreach (var name in minimizeReport.unused)
        {
            if (name != null)
                Console.WriteLine($"{name}\n");
            else
            {
                Console.WriteLine("??null\n");
            }
        }

        return 0;
    }
}