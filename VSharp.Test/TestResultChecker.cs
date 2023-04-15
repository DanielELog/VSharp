using System;
using System.IO;
using System.Linq;
using System.Reflection;

using static VSharp.CoverageRunner.CoverageRunner;
using static VSharp.TestRunner.TestRunner;

namespace VSharp.Test;

public static class TestResultChecker
{
    public static bool Check(DirectoryInfo testDir)
    {
        // TODO: may need 'try/catch'
        // TODO: need to redirect 'Console.Out' and 'Console.Error' to Logger
        return ReproduceTests(testDir);
    }

    public static bool Check(
        DirectoryInfo testDir,
        MethodInfo methodInfo,
        int expectedCoverage,
        out int actualCoverage,
        out string resultMessage)
    {
        var testRunnerPath =
            new DirectoryInfo(Directory.GetCurrentDirectory())
                .EnumerateFiles($"{typeof(TestRunner.TestRunner).FullName}.dll")
                .Single()
                .FullName;
        var runnerWithArgs = $"{testRunnerPath} {testDir.FullName}";
        var (coverage, message) =
            RunAndGetHistory(runnerWithArgs, "cov.cov", testDir.FullName, methodInfo);
        actualCoverage = coverage;
        resultMessage = string.Empty;

        if (expectedCoverage == coverage)
        {
            return true;
        }

        resultMessage = $"Incomplete coverage! Expected {expectedCoverage}, but got {coverage}";
        return false;
    }
}
