using System.Xml;
using NUnit;
using NUnit.Engine;
using NUnit.Engine.Extensibility;

namespace NUnitTestRunner;

internal record TestCase(string Time, string Name, string End);

[Extension(Description = "Test Reporter Extension", EngineVersion="3.17")]
public class TestProgressReporter : ITestEventListener
{
    private readonly TextWriter _writer;

    private void PrintOnTestStart(XmlNode _event)
    {
        _writer.WriteLine($"Test [{_event.GetAttribute("fullname")}] started! {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
    }

    private void PrintOnTestFinish(XmlNode _event)
    {
        _writer.WriteLine(
            $"Test [{_event.GetAttribute("fullname")}] got the result [{_event.GetAttribute("result")}] in {_event.GetAttribute("duration")}s! {DateTime.Now.ToString("HH:mm:ss.ffffff")}");
    }

    public TestProgressReporter(TextWriter writer)
    {
        _writer = writer;
    }
    
    public void OnTestEvent(string report)
    {
        var node = new XmlDocument();
        node.LoadXml(report);
        var eventNode = node.ChildNodes[0];
        if (eventNode is null) return;
        switch (eventNode.Name)
        {
            case "start-test":
                PrintOnTestStart(eventNode);
                break;
            case "test-case":
                PrintOnTestFinish(eventNode);
                break;
        }
    }
}

public static class NUnitTestRunnerProgram
{
    private static readonly List<TestCase> TestCases = new();

    private static void FindTestCases(XmlNode node)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            switch (child.Name)
            {
                case "test-suite":
                    FindTestCases(child);
                    break;
                case "test-case":
                    TestCases.Add(new TestCase(
                        child.GetAttribute("start-time"),
                        child.GetAttribute("fullname"),
                        child.GetAttribute("end-time")));
                    break;
            }
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            Console.WriteLine("expected two strings: path to the test project assembly file and name of the result file; aborting.");
            return -1;
        }
        
        var testProjectPath = args[0];
        var resultName = args[1];

        System.Reflection.Assembly.LoadFrom(testProjectPath);
        
        var engine = TestEngineActivator.CreateInstance();
        
        var package = new TestPackage(testProjectPath);
        
        package.AddSetting("NumberOfTestWorkers", 1);

        using var runner = engine.GetRunner(package);
        
        var filterService = engine.Services.GetService<ITestFilterService>();
        var builder = filterService.GetTestFilterBuilder();
        builder.SelectWhere($"method =~ /.*Tricky.*/");
        var filter = builder.GetFilter();

        Console.WriteLine($"Tests found: {runner.CountTestCases(filter)}");

        var testResult = runner.Run(new TestProgressReporter(Console.Out), filter);
        
        FindTestCases(testResult);
        
        using var sw = new StreamWriter(resultName);
        foreach (var test in TestCases.OrderBy(test => test.Time))
        {
            sw.WriteLine($"{test.Name}");
        }

        return 0;
    }
}