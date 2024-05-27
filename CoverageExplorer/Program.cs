namespace CoverageExplorer;

using VSharp;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("expected one and only string as the path to the coverage file; aborting.");
            return -1;
        }
        
        var cov = File.ReadAllBytes(args[0]);
        var report = CoverageDeserializer.getRawReports(cov);
        if (report == null)
        {
            Console.WriteLine("could not deserialize the coverage; aborting.");
            return -2;
        }

        var viewer = new CoverageViewer(report);

        var exitFlagRaised = false;
        var executor = new CommandExecutor(viewer, () => exitFlagRaised = true);
        
        Console.WriteLine("entered the report, explore with your commands\n");

        while (!exitFlagRaised)
        {
            Console.WriteLine("Currently on:");
            Console.WriteLine(viewer.GetCurrentLoc());
            Console.WriteLine();
            
            var cmdRawArgs = Console.ReadLine()!.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (cmdRawArgs.Length <= 0)
                continue;

            Console.WriteLine(executor.Execute(cmdRawArgs[0], cmdRawArgs[1..]));
            Console.WriteLine();
        }

        return 0;
    }
}