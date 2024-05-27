using System.Reflection;
using System.Linq;

namespace CoverageExplorer;

using VSharp;

public enum CoverageEvents
{
    EnterMain,
    Enter,
    LeaveMain,
    Leave,
    BranchHit,
    Call,
    Tailcall,
    TrackCoverage,
    StsfldHit,
    ThrowLeave,
}

public static class CoverageEventsExtension
{
    public static bool IsLeave(this CoverageEvents cEvent)
    {
        return cEvent is CoverageEvents.Leave or CoverageEvents.LeaveMain or CoverageEvents.ThrowLeave;
    }
    
    public static bool IsEnter(this CoverageEvents cEvent)
    {
        return cEvent is CoverageEvents.Enter or CoverageEvents.EnterMain;
    }
}

public class CoverageViewer
{
    private Dictionary<int, MethodBase> _methods;
    private readonly Dictionary<string, int> _methodIdByName;
    private readonly Dictionary<int, string> _methodNameById;
    private readonly HashSet<int> _ignoredIds;
    
    public readonly struct ViewerLocation
    {
        internal CoverageViewer Owner { get; init; }
        internal RawCoverageLocation Location { get; init; }
        public int StackPos { get; init; }
        public ulong ThreadId => Location.threadId;
        public int MethodId => Location.methodId;
        public string MethodName => Owner._methodNameById[MethodId];
        public uint Offset => Location.offset;
        public CoverageEvents Event => (CoverageEvents)Location.@event;

        public string TimeStamp
        {
            get
            {
                var date = new DateTime(1970, 1, 1);
                date = date.AddMicroseconds(Location.timeInMicroseconds);
                return date.ToString("O");
            }
        }

        public override string ToString()
        {
            if (Event == CoverageEvents.ThrowLeave) return "### meta: left due to unhandled exception; ###";
            return String.Join("\n", MethodName, $"    Offset set on: {Offset} as {Event}", $"    Stack position: {StackPos}", $"    At: {TimeStamp}", "");
        }
    }
    
    private ViewerLocation[][] _reports;

    private int _currentReportId = 0;
    private int _currentLocId = 0;

    private ViewerLocation[] CurrentReport => _reports[_currentReportId];
    private ViewerLocation CurrentLoc => CurrentReport[_currentLocId];

    private string GetReturnType(MethodBase mBase)
    {
        var mInfo = mBase as MethodInfo;
        return mInfo == null ? "" : mInfo.ReturnType.Name;
    }

    private string GetCommonMethodName(MethodBase mBase)
    {
        var param = mBase.GetParameters()
            .Select(param =>
            {
                var res = param.Name;
                if (res == null) return "???";
                return res;
            });
        if (mBase.DeclaringType.ToString().Contains("SimpleClass"))
            Console.WriteLine($"found simpleclass: {mBase.DeclaringType}");
        return $"{GetReturnType(mBase)} {mBase.DeclaringType}.{mBase.Name}({String.Join(", ", param)});";
    }

    private void StoreMethods(Dictionary<int, RawMethodInfo> rawMethodInfos)
    {
        foreach (var kvp in rawMethodInfos)
        {
            var methodId = kvp.Key;
            var mInfo = kvp.Value;

            Assembly? assembly;
            try
            {
                assembly = AssemblyManager.LoadFromAssemblyPath(mInfo.moduleName);
            }
            catch (FileLoadException _)
            {
                Console.WriteLine($"method ignored, module: {mInfo.moduleName}");
                _ignoredIds.Add(methodId);
                continue;
            }

            if (assembly == null) throw new FileLoadException($"could not load assembly {mInfo.moduleName}");

            var moduleName = assembly.Modules.First().FullyQualifiedName;

            var methodBase = Reflection.resolveMethodBaseFromAssembly(assembly, moduleName, (int)mInfo.methodToken);

            _methods[methodId] = methodBase;

            var commonName = GetCommonMethodName(methodBase);
            _methodIdByName[commonName] = methodId;
            _methodNameById[methodId] = commonName;
        }
    }

    private void ReadLocations(RawCoverageReport[] rawReports)
    {
        var reportsCount = rawReports.Length;
        
        for (int i = 0; i < reportsCount; i++)
        {
            var rawLocs = rawReports[i].rawCoverageLocations
                .Where(loc => !_ignoredIds.Contains(loc.methodId) && (((CoverageEvents)loc.@event).IsEnter() || ((CoverageEvents)loc.@event).IsLeave()))
                .ToArray();
            var rawLocsCount = rawLocs.Length;
            var viewerReport = new ViewerLocation[rawLocsCount];
            var currentStack = 0;

            for (int j = 0; j < rawLocsCount; j++)
            {
                var rawLoc = rawLocs[j];

                if (((CoverageEvents)rawLoc.@event).IsEnter())
                {
                    currentStack++;
                }

                viewerReport[j] = new ViewerLocation
                {
                    Owner = this,
                    Location = rawLoc,
                    StackPos = currentStack
                };

                if (((CoverageEvents)rawLoc.@event).IsLeave())
                {
                    currentStack--;
                }
            }

            _reports[i] = viewerReport;
        }
    }

    public CoverageViewer(RawCoverageReports report)
    {
        _methods = new Dictionary<int, MethodBase>();
        _methodIdByName = new Dictionary<string, int>();
        _methodNameById = new Dictionary<int, string>();
        _reports = new ViewerLocation[report.reports.Length][];
        _ignoredIds = new HashSet<int>();
        
        StoreMethods(report.methods);
        ReadLocations(report.reports);
    }
    
    private bool CheckPositionBounds(int reportId, int locId)
    {
        if (reportId >= _reports.Length)
            return false;

        if (locId >= _reports[reportId].Length)
            return false;
        
        return true;
    }

    public bool SetPositionTo((int, int) newPos)
    {
        var (newReport, newLocation) = newPos;
        
        if (!CheckPositionBounds(newReport, newLocation))
            return false;

        _currentReportId = newReport;
        _currentLocId = newLocation;

        return true;
    }

    public string? GetMethodName(int methodId)
    {
        return _methodNameById.ContainsKey(methodId) ? _methodNameById[methodId] : null;
    }

    public bool SingleStep()
    {
        if (_currentLocId == CurrentReport.Length - 1)
            return false;

        _currentLocId++;

        return true;
    }

    public bool BackwardsStep()
    {
        if (_currentLocId == 0)
            return false;

        _currentLocId--;

        return true;
    }

    public bool StepOut()
    {
        var stackBefore = CurrentLoc.StackPos;
        bool success = true;

        while (CurrentLoc.StackPos >= stackBefore && success)
        {
            success = SingleStep();
        }

        return success;
    }

    public bool StepBackOut()
    {
        var stackBefore = CurrentLoc.StackPos;
        bool success = true;

        while (CurrentLoc.StackPos >= stackBefore && success)
        {
            success = BackwardsStep();
        }

        return success;
    }

    public bool StepOver()
    {
        var currentStack = CurrentLoc.StackPos;
        bool success = SingleStep();

        while (success && CurrentLoc.StackPos > currentStack)
        {
            success = SingleStep();
        }

        return success;
    }

    public List<(int, string)> FindMethodsByName(string partial)
    {
        return _methodIdByName
            .Where(nameAndId => nameAndId.Key.Contains(partial, StringComparison.CurrentCultureIgnoreCase))
            .Select(nameAndId => (nameAndId.Value, nameAndId.Key))
            .ToList();
    }

    public List<(int, int)> FindLocsOfMethod(int methodId)
    {
        var occurrences = new List<(int, int)>();

        for (var curReport = 0; curReport < _reports.Length; curReport++)
        {
            var curLocs = _reports[curReport];
            for (var curLoc = 0; curLoc < curLocs.Length; curLoc++)
            {
                var loc = curLocs[curLoc];
                if (loc.MethodId == methodId && loc.Event.IsEnter())
                {
                    occurrences.Add((curReport, curLoc));
                }
            }
        }

        return occurrences;
    }

    public List<(int, int)>? FindLocsOfMethod(string name)
    {
        return !_methodIdByName.ContainsKey(name) ? null : FindLocsOfMethod(_methodIdByName[name]);
    }

    public ViewerLocation? GetLocationAt((int, int) pos)
    {
        var (reportId, locationId) = pos;

        if (!CheckPositionBounds(reportId, locationId))
            return null;

        return _reports[reportId][locationId];
    }

    public List<ViewerLocation> GetStackCalls(int n = -1)
    {
        var stackSize = CurrentLoc.StackPos;
        if (n < 0) n = stackSize;
        var locId = _currentLocId;
        var res = new List<ViewerLocation>();
        var curReport = CurrentReport;

        while (locId >= 0 && n > 0)
        {
            var curLoc = curReport[locId];
            
            if (curLoc.Event.IsEnter() && curLoc.StackPos == stackSize)
            {
                res.Add(curLoc);
                stackSize--;
                n--;
            }

            locId--;
        }

        res.Reverse();
        return res;
    }

    public List<ViewerLocation> GetLocationN(int n = -1)
    {
        var locId = _currentLocId;
        var res = new List<ViewerLocation>();
        var curReport = CurrentReport;

        while (locId >= 0 && n > 0)
        {
            var curLoc = curReport[locId];
            
            res.Add(curLoc);

            n--;
            locId--;
        }

        res.Reverse();
        return res;
    }

    public ViewerLocation GetCurrentLoc()
    {
        return CurrentLoc;
    }

    public (int, int) GetCurrentPosition()
    {
        return (_currentReportId, _currentLocId);
    }
}