using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace VSharp.CoverageRunner
{
    public static class CoverageRunner
    {
        private static readonly string ResultName = "coverage.cov";

        // TODO: 'resultPath' and 'workingDirectory' should be unified
        // TODO: after debug 'Tuple<int, string>' should be changed to 'int'
        public static Tuple<int, string> RunAndGetHistory(string args, string resultPath, string workingDirectory, MethodInfo method)
        {
            string extension;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                extension = ".dll";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                extension = ".so";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                extension = ".dylib";
            else
                return new Tuple<int, string>(-1, "unknown platform");
            var pathToClient = $"libvsharpConcolic{extension}";
            // var profiler = $"%s{Directory.GetCurrentDirectory()}%c{Path.DirectorySeparatorChar}%s{pathToClient}";
            var profiler =
                $"{Directory.GetCurrentDirectory()}/../../../../VSharp.Fuzzer/VSharp.ClrInteraction/cmake-build-debug/{pathToClient}";

            var info = new ProcessStartInfo
            {
                EnvironmentVariables =
                {
                    ["CORECLR_PROFILER"] = "{2800fea6-9667-4b42-a2b6-45dc98e77e9e}",
                    ["CORECLR_ENABLE_PROFILING"] = "1",
                    ["CORECLR_PROFILER_PATH"] = profiler,
                    ["COVERAGE_ENABLE_PASSIVE"] = "1",
                    ["COVERAGE_RESULT_PATH"] = resultPath,
                    ["COVERAGE_METHOD_ASSEMBLY_NAME"] = method.Module.Assembly.FullName,
                    ["COVERAGE_METHOD_MODULE_NAME"] = method.Module.FullyQualifiedName,
                    ["COVERAGE_METHOD_TOKEN"] = method.MetadataToken.ToString()
                },
                WorkingDirectory = workingDirectory,
                FileName = "dotnet",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true
            };

            var proc = Process.Start(info);
            if (proc == null)
                return new Tuple<int, string>(-3, "couldn't generate dotnet process");
            proc.WaitForExit();

            // var covHistory = File.ReadAllBytes(ResultFullPath);

            var exit = proc.ExitCode;

            return new Tuple<int, string>(exit, proc.StandardError.ReadToEnd());
        }
    }
}
