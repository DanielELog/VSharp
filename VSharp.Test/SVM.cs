using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using VSharp.Core;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Microsoft.FSharp.Core;

namespace VSharp.Test
{
    public class SVM
    {
        private ExplorerBase _explorer;
        private Statistics _statistics = new Statistics();

        public SVM(ExplorerBase explorer)
        {
            _explorer = explorer;
            API.Configure(explorer);
        }

        private codeLocationSummary PrepareAndInvoke(IDictionary<MethodInfo, codeLocationSummary> dict, MethodInfo m,
            Func<IMethodIdentifier, FSharpFunc<codeLocationSummary, codeLocationSummary>, codeLocationSummary> invoke)
        {
            try
            {
                _statistics.SetupBeforeMethod(m);
                IMethodIdentifier methodIdentifier = _explorer.MakeMethodIdentifier(m);
                if (methodIdentifier == null)
                {
                    var format =
                        new PrintfFormat<string, Unit, string, Unit>(
                            $"WARNING: metadata method for {m.Name} not found!");
                    Logger.printLog(Logger.Warning, format);
                    return null;
                }

                dict?.Add(m, null);
                var id = FSharpFunc<codeLocationSummary, codeLocationSummary>.FromConverter(x => x);
                var summary = invoke(methodIdentifier, id);
                _statistics.AddSucceededMethod(m);
                if (dict != null)
                {
                    dict[m] = summary;
                }

                return summary;
            }
            catch (Exception e)
            {
                _statistics.AddException(e, m);
            }

            return null;
        }

        private void InterpretEntryPoint(IDictionary<MethodInfo, codeLocationSummary> dictionary, MethodInfo m)
        {
            Assert.IsTrue(m.IsStatic);
            PrepareAndInvoke(dictionary, m, _explorer.InterpretEntryPoint);
        }

        private void Explore(IDictionary<MethodInfo, codeLocationSummary> dictionary, MethodInfo m)
        {
            if (m.GetMethodBody() != null)
                PrepareAndInvoke(dictionary, m, _explorer.Explore);
        }

        private void ExploreType(List<string> ignoreList, MethodInfo ep,
            IDictionary<MethodInfo, codeLocationSummary> dictionary, Type t)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                        BindingFlags.DeclaredOnly;

            if (ignoreList?.Where(kw => !t.AssemblyQualifiedName.Contains(kw)).Count() == ignoreList?.Count &&
                t.IsPublic)
            {
                foreach (var m in t.GetMethods(bindingFlags))
                {
                    // FOR DEBUGGING SPECIFIED METHOD
                    // if (m != ep && !m.IsAbstract)
                    if (m != ep && !m.IsAbstract && m.Name != "op_Division")
                    {
                        Debug.Print(@"Called interpreter for method {0}", m.Name);
                        Explore(dictionary, m);
                    }
                }
            }
        }

        private static string ReplaceLambdaLines(string str)
        {
            return Regex.Replace(str, @"@\d+(\+|\-)\d*\[Microsoft.FSharp.Core.Unit\]", "");
        }

        private static string ResultToString(codeLocationSummary summary)
        {
            if (summary == null)
                return "summary is null";
            return $"{summary.result}\nHEAP:\n{ReplaceLambdaLines(API.Memory.Dump(summary.state))}";
        }

        public string ExploreOne(MethodInfo m)
        {
            var summary = PrepareAndInvoke(null, m, _explorer.Explore);
            return ResultToString(summary);
        }

        public void ConfigureSolver(ISolver solver)
        {
            // API.ConfigureSolver(solver);
        }

        public IDictionary<MethodInfo, string> Run(Assembly assembly, List<string> ignoredList)
        {
            IDictionary<MethodInfo, codeLocationSummary> dictionary = new Dictionary<MethodInfo, codeLocationSummary>();
            var ep = assembly.EntryPoint;

            foreach (var t in assembly.GetTypes())
            {
                ExploreType(ignoredList, ep, dictionary, t);
            }

            if (ep != null)
            {
                InterpretEntryPoint(dictionary, ep);
            }

            _statistics.SaveExceptionsShortStats();

            return dictionary.ToDictionary(kvp => kvp.Key, kvp => ResultToString(kvp.Value));
        }
    }
}
