using System.Reflection;
using Array = System.Array;

namespace CoverageExplorer;

internal class CommandRouter
{
    private readonly CoverageViewer _viewer;
    private readonly Action _killSwitch;
    private readonly string _docs;
    private string _searchedMethod = string.Empty;
    private Dictionary<int, int[]> _searchResults = new();

    private static string TryStep(Func<bool> stepping)
    {
        return !stepping() ? "end of report; nowhere to go" : "";
    }

    internal CommandRouter(CoverageViewer viewer, Action killSwitch, string docs)
    {
        _viewer = viewer;
        _killSwitch = killSwitch;
        _docs = docs;
    }

    internal string __GetLoc(int reportId, int locationId)
    {
        var loc = _viewer.GetLocationAt((reportId, locationId));
        if (loc == null) return $"no location present at ({reportId}, {locationId})";
        return ((CoverageViewer.ViewerLocation)loc).ToString();
    }

    internal string __Exit()
    {
        _killSwitch();
        return "exiting...";
    }

    internal string __Help()
    {
        return _docs;
    }

    internal string __GetStack(int n)
    {
        return string.Join("\n",
            _viewer.GetStackCalls(n)
                .Select(loc => loc.MethodName));
    }

    internal string __GetStack()
    {
        return __GetStack(-1);
    }

    internal string __GetLocN(int n)
    {
        return string.Join("\n",
            _viewer.GetLocationN(n)
                .Select(loc => $"{loc.Event}: {loc.MethodName}"));
    }

    internal string __SetPos(int recordId, int locationId)
    {
        var success = _viewer.SetPositionTo((recordId, locationId));
        return success ? "" : $"!   no location at position ({recordId}, {locationId})";
    }

    internal string __GetMethodName(int methodId)
    {
        var res = _viewer.GetMethodName(methodId);
        return res ?? "not found";
    }

    internal string __Find(string partialName)
    {
        var matches = _viewer.FindMethodsByName(partialName);
        if (matches.Count == 0) return "no matches found";
        (int, string) md;
        if (matches.Count > 1)
        {
            Console.WriteLine("multiple methods found, choose by id from below:");
            Console.WriteLine(string.Join("\n", matches.Select(match => $"{match.Item1}: {match.Item2}")));
            Console.WriteLine();
            if (!int.TryParse(Console.ReadLine()!, out var id))
                return "incorrect id, returning;";
            md = matches.FirstOrDefault(m => m.Item1 == id, (-1, ""));
            if (md.Item1 == -1)
                return "entered id is not present in found; returning";
        }
        else
        {
            md = matches[0];
        }

        _searchedMethod = md.Item2;
        var found = _viewer.FindLocsOfMethod(md.Item1);
        var mapper = new Dictionary<int, List<int>>();
        _searchResults = new Dictionary<int, int[]>();
        foreach (var find in found)
        {
            var (reportId, locId) = find;
            if (mapper.TryGetValue(reportId, out var value))
                value.Add(locId);
            else
                mapper[reportId] = new List<int> { locId };
        }

        foreach (var (reportId, locs) in mapper)
            _searchResults[reportId] = locs.Order().ToArray();

        return "First location matches in each report:\n"
               + string.Join("\n", _searchResults.Select(repAndLocs => $"({repAndLocs.Key}, {repAndLocs.Value[0]})"));
    }

    internal string __FindNext()
    {
        var (curReport, curLoc) = _viewer.GetCurrentPosition();
        if (!_searchResults.ContainsKey(curReport))
            return $"no matches for [{_searchedMethod}] found in the current report!";
        var nextPos = _searchResults[curReport].FirstOrDefault(locId => locId > curLoc, -1);
        if (nextPos == -1)
            return $"reached end for the search of [{_searchedMethod}] in the current report!";
        _viewer.SetPositionTo((curReport, nextPos));
        return "";
    }

    internal string __FindPrev()
    {
        var (curReport, curLoc) = _viewer.GetCurrentPosition();
        if (!_searchResults.ContainsKey(curReport))
            return $"no matches for [{_searchedMethod}] found in the current report!";
        var nextPos = _searchResults[curReport].Reverse().FirstOrDefault(locId => locId < curLoc, -1);
        if (nextPos == -1)
            return $"reached end for the search of [{_searchedMethod}] in the current report!";
        _viewer.SetPositionTo((curReport, nextPos));
        return "";
    }

    internal string __ShowAll()
    {
        return string.Join("\n",
            _searchResults
            .Select(repAndLocs => $"{repAndLocs.Key}: {string.Join(", ", repAndLocs.Value)}"));
    }

    internal string __Next()
    {
        return TryStep(_viewer.SingleStep);
    }

    internal string __Next(int n)
    {
        for (; n > 0 && _viewer.SingleStep(); n--) {}

        return n > 0 ? "reached the end of report" : "";
    }

    internal string __GetCur()
    {
        return _viewer.GetCurrentLoc().ToString();
    }

    internal string __StepOut()
    {
        return TryStep(_viewer.StepOut);
    }

    internal string __StepOver()
    {
        return TryStep(_viewer.StepOver);
    }

    internal string __StepBack()
    {
        return TryStep(_viewer.BackwardsStep);
    }

    internal string __StepBack(int n)
    {
        for (; n > 0 && _viewer.BackwardsStep(); n--) {}

        return n > 0 ? "reached the start of report" : "";
    }

    internal string __StepBackOut()
    {
        return TryStep(_viewer.StepBackOut);
    }
}

internal class CommandExecutor
{
    internal record CommandInfo
    {
        internal required MethodInfo Func { get; init; }
        internal required Type[] ArgTypes { get; init; }
    }

    private readonly CommandRouter _cmdInstance;
    private readonly Dictionary<string, List<CommandInfo>> _commands;

    private string InitializeAvailableCommands()
    {
        var docs = string.Empty;

        foreach (var method in typeof(CommandRouter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            var mdName = method.Name.ToLower();
            if (mdName.Length < 2 || mdName[..2] != "__") continue;

            var args = method.GetParameters();
            CommandInfo cmdInfo = new()
            {
                Func = method,
                ArgTypes = args.Select(arg => arg.ParameterType).ToArray()
            };
            docs += $"{mdName[2..]}({string.Join(", ", args.Select(arg => arg.Name))})\n";
            if (_commands.TryGetValue(mdName, out var command))
                command.Add(cmdInfo);
            else
                _commands[mdName] = new List<CommandInfo> { cmdInfo };
        }

        return docs;
    }

    internal CommandExecutor(CoverageViewer viewer, Action killSwitch)
    {
        _commands = new Dictionary<string, List<CommandInfo>>();
        var docs = InitializeAvailableCommands();
        _cmdInstance = new CommandRouter(viewer, killSwitch, docs);
    }

    private bool TryConvertArg(Type type, string arg, ref object obj)
    {
        if (type == typeof(int))
        {
            if (!int.TryParse(arg, out var num)) return false;
            obj = num;
            return true;
        }

        if (type == typeof(string))
        {
            obj = arg;
            return true;
        }

        throw new InvalidOperationException($"Trying to convert unsupported type [{type}]! Aborting");
    }

    private bool TryConvertArguments(CommandInfo cmdInfo, string[] args, out object[] converted)
    {
        var count = cmdInfo.ArgTypes.Length;
        if (count != args.Length)
        {
            converted = System.Array.Empty<object>();
            return false;
        }

        var success = true;
        converted = new object[count];
        for (var i = 0; i < count && success; i++)
            success = TryConvertArg(cmdInfo.ArgTypes[i], args[i], ref converted[i]);

        return success;
    }

    internal string Execute(string cmdName, string[] argsRaw)
    {
        var internalCmdName = $"__{cmdName.ToLower()}";
        if (!_commands.ContainsKey(internalCmdName))
            return "could not parse the command";
        
        var args = Array.Empty<object>();
        var cmdInfo = _commands[$"__{cmdName}"]
            .FirstOrDefault(cmdInfo => TryConvertArguments(cmdInfo, argsRaw, out args));

        if (cmdInfo == null)
            return "could not parse the parameters";

        var res = (string?)cmdInfo.Func.Invoke(_cmdInstance, args);
        return res ?? "could not invoke the command";
    }
}