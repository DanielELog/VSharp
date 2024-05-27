namespace VSharp.TestProcessing

open System
open System.Linq
open System.Collections
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Reflection
open FSharpx.Collections
open Microsoft.FSharp.Core
open VSharp.CSharpUtils
open VSharp

open NUnit.Framework
open Xunit
open Microsoft.VisualStudio.TestTools.UnitTesting

type TestProcessingAPI(projectRootDirectory: DirectoryInfo, useCachedCoverages: bool) =

    let projectAssemblies =
        lazy(
            let assemblies = AssembliesCollector.GetTestAndUserLibraries(projectRootDirectory)
            if assemblies.IsNone
            then Prelude.internalfailf $"could not load assemblies for the specified project: {projectRootDirectory.Name}"
            else assemblies.Value
        )
        
    let assemblyReports =
        lazy
            if useCachedCoverages
            then CoverageCollector.RetrieveAssemblyReport("coverage.cov", "order.log")
            else CoverageCollector.Collect(projectAssemblies.Value)
        
    // TODO: move coverage parsing and restructuring out of API?    
    static let testAttributesList = [
        //// NUNIT TEST ATTRIBUTES
        typeof<TestAttribute>
        typeof<TestCaseAttribute>
        typeof<TestCaseSourceAttribute>
        typeof<NUnit.Framework.TheoryAttribute>
        
        //// XUNIT TEST ATTRIBUTES
        typeof<FactAttribute>
        typeof<Xunit.TheoryAttribute>
        
        //// MSTEST TEST ATTRIBUTES
        typeof<TestMethodAttribute>
    ]
    
    // NOTE: non-exhaustive!
    static let nonTestAttributesList = [
        //// NUNIT
        typeof<TearDownAttribute>
        typeof<SetUpAttribute>
    ]
    
    static let testAttributesFullNames = lazy(testAttributesList |> List.map (fun attr -> attr.FullName))
    static let nonTestAttributesFullNames = lazy(nonTestAttributesList |> List.map (fun attr -> attr.FullName))
    
    // distinguishes SetUp and TearDown of XUnit Framework
    let isXUnitPreTestMethod (method: MethodBase) =
        // NOTE: the restriction might be too broad
        method.IsConstructor || method.Name = "Dispose"
        
    let isMethodATest (method: MethodBase) =
        // checks if the method is marked as a test directly
        method.CustomAttributes
        |> Seq.cast<CustomAttributeData>
        |> Seq.exists (fun x ->
            List.contains x.AttributeType.FullName testAttributesFullNames.Value
        )
        // TODO: this will mark any other custom TestCommand, including SetUp, TearDown etc
        // checks for the custom test runner execution methods
        || (method.Name = "Execute" && method.DeclaringType.BaseType.AssemblyQualifiedName = typeof<NUnit.Framework.Internal.Commands.DelegatingTestCommand>.AssemblyQualifiedName)
        
    let methodIdByIdentification = DictionaryNoOverwrite<UniqueMethodIdentification, int>()
    let isATestByMethodId = Dictionary<int, bool>()
    let assemblyByName = DictionaryNoOverwrite<string, Assembly>()
    let methodBaseById = Dictionary<int, MethodBase>()
    let coverageByTestId = Dictionary<int, HashSet<SimplifiedLocation>>()
    let methodIdByTestId = Dictionary<int, int>()
    let testNameById = Dictionary<int, string>()
    let mutable currentGlobalTestId = 0
    
    let getAssembly assemblyPath =
        let asmFromAlc = AssemblyManager.LoadFromAssemblyPath assemblyPath
        let asmName = asmFromAlc.FullName
        assemblyByName.SetOrKeepAndGet(asmName, (fun () -> asmFromAlc))
    
    let mutable nextMethodId = 0
    
    let storeMethodBase (methodBase: MethodBase) =
        let addNewMethodBase() =
            methodBaseById[nextMethodId] <- methodBase
            isATestByMethodId[nextMethodId] <- isMethodATest methodBase
            nextMethodId <- nextMethodId + 1
            nextMethodId - 1

        let identification =
            {
                token = methodBase.MetadataToken
                asmName = methodBase.Module.Assembly.FullName
            }
            
        methodIdByIdentification.SetOrKeepAndGet(identification, addNewMethodBase)
        
    let microsecondsToDate (time: int64) =
        let mutable date = DateTime(1970, 1, 1)
        date <- date.AddMicroseconds(time |> double)
        date.ToString("O")

    let unifySingleAssemblyReport (report: AssemblyReport) =
        let methodsOfReports = report.reports.methods
        let coverageReports = report.reports.reports
        let testNames = report.testMethods
        let mutable curNameId = 0
        let testNamesLength = testNames.Length
            
        let localIdToGlobalId = Dictionary<int, int>()
        for KeyValue(localId, methodInfo) in methodsOfReports do
            let assembly = getAssembly methodInfo.moduleName
            
            // TODO: review the case of multiple modules inside one assembly?
            let moduleName = assembly.Modules.First().FullyQualifiedName
            
            let methodBase = Reflection.resolveMethodBaseFromAssembly assembly moduleName (int32 methodInfo.methodToken)
            localIdToGlobalId[localId] <- storeMethodBase methodBase

        let addCoverageReport report testMethodId testId =
            if coverageByTestId.ContainsKey(testId) |> not
            then
                coverageByTestId[testId] <- HashSet<SimplifiedLocation>()
            let coverage = coverageByTestId[testId]
            
            report.rawCoverageLocations
            (*TODO: improve filtering system
              NOTE: this filtering was supposed to ignore test's code itself
                    however, the first user-space method visited in a report
                    does not correspond with that
                    look up custom TestRunners
            *)
            |> Seq.filter (fun loc -> loc.methodId <> testMethodId)
            |> Seq.iter (fun loc ->
                {
                    methodId = localIdToGlobalId[loc.methodId]
                    offset = loc.offset
                }
                |> coverage.Add |> ignore
            )
            
        let mutable currentTestId = None
        let mutable currentTestMethodId = 0
        let mutable currentTestEnd = 0L
            
        let determineTestAndAddReport (report: RawCoverageReport) =
            let firstLoc = report.rawCoverageLocations[0]
            let mainMethodId = firstLoc.methodId
            let mBase = methodBaseById[localIdToGlobalId[mainMethodId]]
            let isATest = isATestByMethodId[localIdToGlobalId[mainMethodId]]
            let curReportTimeStart = firstLoc.timeInMicroseconds
            let curReportTimeEnd = report.rawCoverageLocations[report.rawCoverageLocations.Length - 1].timeInMicroseconds
            
            // exited from the current test
            if curReportTimeStart > currentTestEnd
            then currentTestId <- None
                        
            // NOTE: disabling the next checks because the current implementation
            //       of isMethodATest considers not being a test only those
            //       methods that are flagged by a non-test attribute
            
            // // next test started when the previous hasn't finished yet
            // if isATest && currentTestId.IsSome
            // then
            //     let strOfCurTestId = MethodWriter.GetString(methodBaseById[localIdToGlobalId[currentTestId.Value]])
            //     let strOfNewTestId = MethodWriter.GetString(methodBaseById[localIdToGlobalId[mainMethodId]])
            //     let generalErrorMsg = "TestProcessingAPI found concurrent execution of tests, which is unsupported!"
            //     let errorTestsMessage = $"Concurrently ran:\n    [{strOfCurTestId}]\n    and\n    [{strOfNewTestId}]"
            //     Prelude.internalfail $"{generalErrorMsg}\n{errorTestsMessage}\naborting..."
            
            // new test reports received
            // IsNone check is terrible, but currently there's no way of determining every test method enter...
            if isATest && currentTestId.IsNone
            then
                testNameById[currentGlobalTestId] <- testNames[curNameId]
                methodIdByTestId[currentGlobalTestId] <- localIdToGlobalId[mainMethodId]
                currentTestMethodId <- mainMethodId
                curNameId <- curNameId + 1
                currentTestId <- Some currentGlobalTestId
                currentGlobalTestId <- currentGlobalTestId + 1
                currentTestEnd <- curReportTimeEnd
                
            // adding the report; if it's outside of any test execution, ignore it
            currentTestId
            |> Option.iter (addCoverageReport report currentTestMethodId)

        coverageReports
        // sorting reports by its first recorded location, determining which reports belong to which test executions
        // should be recorded by the coverage tool in the correct order, may drop in the future
        |> Array.sortBy (fun x -> x.rawCoverageLocations[0].timeInMicroseconds)
        |> Array.iter determineTestAndAddReport
        
    do
        List.iter unifySingleAssemblyReport assemblyReports.Value
        
    let totalCoverage =
        {
            methodsCount = nextMethodId
            testCoverages = coverageByTestId 
        }

    new(projectRootDirectory: DirectoryInfo) = TestProcessingAPI(projectRootDirectory, false)
    
    member this.GetCrosscheckerReport() =
        ()
        
    member this.MinimizeTests() =
        let minimized =
            TestMinimizer.Minimize(totalCoverage)
            
        let unused =
            totalCoverage.testCoverages
            |> Seq.map (fun kvp -> kvp.Key)
            |> Seq.filter (fun x -> (Seq.contains x minimized) |> not)
            
        {
            minimized = minimized |> Seq.map (fun id -> testNameById[id])
            unused = unused |> Seq.map (fun id -> testNameById[id])
        }
        
    member this.EmptyTestRun() =
        let getMethodName methodId =
            MethodWriter.GetString(methodBaseById[methodId])
 
        coverageByTestId
        |> Seq.iter (fun kvp ->
            let testId = kvp.Key
            let locations = kvp.Value
            Console.WriteLine($"Test {testNameById[testId]} ran")
            Console.WriteLine($"    presumable test method: {getMethodName methodIdByTestId[testId]}")
            locations
            |> Seq.iter (fun loc ->
                Console.WriteLine($"    {loc.offset} of {getMethodName loc.methodId}")
            )
            Console.WriteLine()
        )
            
        for KeyValue(id, mBase) in methodBaseById do
            Console.WriteLine($"""{id.ToString("000")}: {mBase.MetadataToken} {getMethodName id}\n\n""")
        Console.WriteLine()
        