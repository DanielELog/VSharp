#nullable enable
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using VSharp.CSharpUtils;

namespace VSharp.Runner
{
    public static class RunnerProgram
    {

        private static Assembly? ResolveAssembly(FileInfo assemblyPath)
        {
            try
            {
                return AssemblyManager.LoadFromAssemblyPath(assemblyPath.FullName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("I did not load assembly {0}; Error:{1}", assemblyPath.FullName, ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return null;
            }
        }

        private static Type? ResolveType(Assembly assembly, string? classArgumentValue)
        {
            if (classArgumentValue == null)
            {
                Console.Error.WriteLine("Specified class can not be null");
                return null;
            }

            var specificClass =
                assembly.GetType(classArgumentValue) ??
                assembly.GetTypes()
                    .Where(t => (t.FullName ?? t.Name).Contains(classArgumentValue))
                    .MinBy(t => t.FullName?.Length ?? t.Name.Length);
            if (specificClass == null)
            {
                Console.Error.WriteLine("I did not found type you specified {0} in assembly {1}", classArgumentValue,
                    assembly.Location);
                return null;
            }

            return specificClass;
        }

        private static MethodBase? ResolveMethod(Assembly assembly, string? methodArgumentValue)
        {
            if (methodArgumentValue == null)
            {
                Console.Error.WriteLine("Specified method can not be null");
                return null;
            }

            MethodBase? method = null;
            if (Int32.TryParse(methodArgumentValue, out var metadataToken))
            {
                foreach (var module in assembly.GetModules())
                {
                    try
                    {
                        method = module.ResolveMethod(metadataToken);
                        if (method != null) break;
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (method == null)
                {
                    Console.Error.WriteLine("I did not found method you specified by token {0} in assembly {1}",
                        metadataToken, assembly.Location);
                    return null;
                }
            }
            else
            {
                foreach (var type in assembly.GetTypes())
                {
                    try
                    {
                        method = type.GetMethod(methodArgumentValue, Reflection.allBindingFlags);
                        method ??= type.GetMethods(Reflection.allBindingFlags)
                            .Where(m => $"{type.FullName}.{m.Name}".Contains(methodArgumentValue))
                            .MinBy(m => m.Name.Length);
                        if (method != null) break;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                if (method == null)
                {
                    Console.Error.WriteLine("I did not found method you specified by name {0} in assembly {1}",
                        methodArgumentValue, assembly.Location);
                    return null;
                }
            }

            return method;
        }

        private static IEnumerable<Type> ResolveNamespace(Assembly assembly, string namespaceArgumentValue)
        {
            if (namespaceArgumentValue == null)
            {
                Console.Error.WriteLine("Specified namespace can not be null");
                return null;
            }

            var types =
                assembly.EnumerateExplorableTypes()
                    .Where(t => t.Namespace?.StartsWith(namespaceArgumentValue) == true);

            if (types.Count() == 0)
            {
                Console.Error.WriteLine($"I did not found any types in namespace {namespaceArgumentValue}");
            }

            return types;
        }

        private static void PostProcess(Statistics statistics)
        {
            statistics.GenerateReport(Console.Out);
        }

        public static int Main(string[] args)
        {
            var assemblyPathArgument =
                new Argument<FileInfo>("assembly-path", description: "Path to the target assembly");
            var timeoutOption =
                new Option<int>(aliases: new[] { "--timeout", "-t" },
                    () => -1,
                    "Time for test generation in seconds. Negative value means no timeout.");
            var solverTimeoutOption =
                new Option<int>(aliases: new[] { "--solver-timeout", "-st" },
                    () => -1,
                    "Timeout for SMT solver in seconds. Negative value means no timeout.");
            var outputOption =
                new Option<DirectoryInfo>(aliases: new[] { "--output", "-o" },
                () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                "Path where unit tests will be generated");
            var concreteArguments =
                new Argument<string[]>("args", description: "Command line arguments");
            var unknownArgsOption =
                new Option("--unknown-args", description: "Force engine to generate various input console arguments");
            var renderTestsOption =
                new Option("--render-tests", description: "Render generated tests as NUnit project to specified output path");
            var singleFileOption =
                new Option("--single-file") { IsHidden = true };
            var searchStrategyOption =
                new Option<SearchStrategy>(aliases: new[] { "--strat" },
                    () => TestGenerator.DefaultSearchStrategy,
                    "Strategy which symbolic virtual machine uses for branch selection.");
            var verbosityOption =
                new Option<Verbosity>(aliases: new[] { "--verbosity", "-v" },
                    () => TestGenerator.DefaultVerbosity,
                    "Determines which messages are displayed in output.");

            var rootCommand = new RootCommand();

            var entryPointCommand =
                new Command("--entry-point", "Generate test coverage from the entry point of assembly (assembly must contain Main method)");
            rootCommand.AddCommand(entryPointCommand);
            entryPointCommand.AddArgument(assemblyPathArgument);
            entryPointCommand.AddArgument(concreteArguments);
            entryPointCommand.AddGlobalOption(timeoutOption);
            entryPointCommand.AddGlobalOption(solverTimeoutOption);
            entryPointCommand.AddGlobalOption(outputOption);
            entryPointCommand.AddOption(unknownArgsOption);
            entryPointCommand.AddGlobalOption(renderTestsOption);
            entryPointCommand.AddGlobalOption(searchStrategyOption);
            entryPointCommand.AddGlobalOption(verbosityOption);
            var allPublicMethodsCommand =
                new Command("--all-public-methods", "Generate unit tests for all public methods of all public classes of assembly");
            rootCommand.AddCommand(allPublicMethodsCommand);
            allPublicMethodsCommand.AddArgument(assemblyPathArgument);
            allPublicMethodsCommand.AddGlobalOption(timeoutOption);
            allPublicMethodsCommand.AddGlobalOption(solverTimeoutOption);
            allPublicMethodsCommand.AddGlobalOption(outputOption);
            allPublicMethodsCommand.AddGlobalOption(renderTestsOption);
            allPublicMethodsCommand.AddOption(singleFileOption);
            allPublicMethodsCommand.AddGlobalOption(searchStrategyOption);
            allPublicMethodsCommand.AddGlobalOption(verbosityOption);
            var publicMethodsOfClassCommand =
                new Command("--type", "Generate unit tests for all public methods of specified class");
            rootCommand.AddCommand(publicMethodsOfClassCommand);
            var classArgument = new Argument<string>("class-name");
            publicMethodsOfClassCommand.AddArgument(classArgument);
            publicMethodsOfClassCommand.AddArgument(assemblyPathArgument);
            publicMethodsOfClassCommand.AddGlobalOption(timeoutOption);
            publicMethodsOfClassCommand.AddGlobalOption(solverTimeoutOption);
            publicMethodsOfClassCommand.AddGlobalOption(outputOption);
            publicMethodsOfClassCommand.AddGlobalOption(renderTestsOption);
            publicMethodsOfClassCommand.AddGlobalOption(searchStrategyOption);
            publicMethodsOfClassCommand.AddGlobalOption(verbosityOption);
            var specificMethodCommand =
                new Command("--method", "Try to resolve and generate unit test coverage for the specified method");
            rootCommand.AddCommand(specificMethodCommand);
            var methodArgument = new Argument<string>("method-name");
            specificMethodCommand.AddArgument(methodArgument);
            specificMethodCommand.AddArgument(assemblyPathArgument);
            specificMethodCommand.AddGlobalOption(timeoutOption);
            specificMethodCommand.AddGlobalOption(solverTimeoutOption);
            specificMethodCommand.AddGlobalOption(outputOption);
            specificMethodCommand.AddGlobalOption(renderTestsOption);
            specificMethodCommand.AddGlobalOption(searchStrategyOption);
            specificMethodCommand.AddGlobalOption(verbosityOption);
            var namespaceCommand =
                new Command("--namespace", "Try to resolve and generate unit test coverage for all public methods of specified namespace");
            rootCommand.AddCommand(namespaceCommand);
            var namespaceArgument = new Argument<string>("namespace-name");
            namespaceCommand.AddArgument(namespaceArgument);
            namespaceCommand.AddArgument(assemblyPathArgument);
            namespaceCommand.AddGlobalOption(timeoutOption);
            namespaceCommand.AddGlobalOption(solverTimeoutOption);
            namespaceCommand.AddGlobalOption(outputOption);
            namespaceCommand.AddGlobalOption(renderTestsOption);
            namespaceCommand.AddGlobalOption(searchStrategyOption);
            namespaceCommand.AddGlobalOption(verbosityOption);

            rootCommand.Description = "Symbolic execution engine for .NET";

            entryPointCommand.Handler = CommandHandler.Create<FileInfo, string[], int, int, DirectoryInfo, bool, bool, SearchStrategy, Verbosity>((assemblyPath, args, timeout, solverTimeout, output, unknownArgs, renderTests, strat, verbosity) =>
            {
                var assembly = ResolveAssembly(assemblyPath);
                var inputArgs = unknownArgs ? null : args;
                if (assembly != null)
                    PostProcess(TestGenerator.Cover(assembly, inputArgs, timeout, solverTimeout, output.FullName, renderTests, strat, verbosity));
            });
            allPublicMethodsCommand.Handler = CommandHandler.Create<FileInfo, int, int, DirectoryInfo, bool, bool, SearchStrategy, Verbosity>((assemblyPath, timeout, solverTimeout, output, renderTests, singleFile, strat, verbosity) =>
            {
                var assembly = ResolveAssembly(assemblyPath);
                if (assembly != null)
                    PostProcess(TestGenerator.Cover(assembly, timeout, solverTimeout, output.FullName, renderTests, singleFile, strat, verbosity));
            });
            publicMethodsOfClassCommand.Handler = CommandHandler.Create<string, FileInfo, int, int, DirectoryInfo, bool, SearchStrategy, Verbosity>((className, assemblyPath, timeout, solverTimeout, output, renderTests, strat, verbosity) =>
            {
                var assembly = ResolveAssembly(assemblyPath);
                if (assembly != null)
                {
                    var type = ResolveType(assembly, className);
                    if (type != null)
                    {
                        PostProcess(TestGenerator.Cover(type, timeout, solverTimeout, output.FullName, renderTests, strat, verbosity));
                    }
                }
            });
            specificMethodCommand.Handler = CommandHandler.Create<string, FileInfo, int, int, DirectoryInfo, bool, SearchStrategy, Verbosity>((methodName, assemblyPath, timeout, solverTimeout, output, renderTests, strat, verbosity) =>
            {
                var assembly = ResolveAssembly(assemblyPath);
                if (assembly != null)
                {
                    var method = ResolveMethod(assembly, methodName);
                    if (method != null)
                    {
                        PostProcess(TestGenerator.Cover(method, timeout, solverTimeout, output.FullName, renderTests, strat, verbosity));
                    }
                }
            });
            namespaceCommand.Handler = CommandHandler.Create<string, FileInfo, int, int, DirectoryInfo, bool, SearchStrategy, Verbosity>((namespaceName, assemblyPath, timeout, solverTimeout, output, renderTests, strat, verbosity) =>
            {
                var assembly = ResolveAssembly(assemblyPath);
                if (assembly != null)
                {
                    var namespaceTypes = ResolveNamespace(assembly, namespaceName);
                    if (namespaceTypes.Count() != 0)
                    {
                        PostProcess(TestGenerator.Cover(namespaceTypes, timeout, solverTimeout, output.FullName, renderTests, strat, verbosity));
                    }
                }
            });

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
