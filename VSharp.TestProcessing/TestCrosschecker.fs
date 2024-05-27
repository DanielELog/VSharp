namespace VSharp.TestProcessing

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Reflection
open FSharpx.Collections
open Microsoft.FSharp.Core
open VSharp.CSharpUtils

open NUnit.Framework
open VSharp.CoverageTool
open Xunit
open Microsoft.VisualStudio.TestTools.UnitTesting

open VSharp

type private TestCrosscheckerTool() =
        
    // composes coverage locations in such a way test methods are easier to compare
    static let recomposeTestMethodCoverage totalMethodCount (reports: RawCoverageReport[]) =
        let testMethodCoverages = Array.create totalMethodCount None
        
        let recordTestMethodRun report =
            // first record is always EnterMain
            let testMethodId = report.rawCoverageLocations[0].methodId
            
            // if a test was run multiple times, add all coverages together
            if testMethodCoverages[testMethodId].IsNone then
                testMethodCoverages[testMethodId] <- Array.create totalMethodCount [] |> Some
                
            report.rawCoverageLocations
            |> Array.iter (fun x ->
                // skipping coverage of the test itself
                if x.methodId <> testMethodId then
                    // when checking within a single test project, we only need offsets of each called method to
                    // list the covered statements
                    testMethodCoverages[testMethodId].Value[x.methodId] <- x.offset :: testMethodCoverages[testMethodId].Value[x.methodId]
            )
        
        reports
        |> Array.iter recordTestMethodRun
        
        testMethodCoverages
        // the following block iterates through coverage of each distinct method covered
        // by each distinct test, then converts to an array and sorts them
        // for more efficient usage of data further on
        |> Array.map (
            Option.map (
                Array.map
                    (fun x ->
                        x
                        |> Array.ofList
                        // sorting by offset lets us walk through them more effectively while checking inclusion
                        
                        // TODO: sorting by descending to find a mismatch quicker? must change findOverlappingCoverages
                        // along in that case
                        |> Array.sort
                        |> Array.distinct // keeping only unique blocks
                )
            )
        )
        
    static let findOverlappingCoverages (testMethodCoverages: TestMethodCoverage option array) =
        let checkIfConsistsIn coverageIdToCheck sampleCoverageId =
            let mutable result = true
            // this method is given ALL methods run in test process
            // TODO: must filter out those marked as `None` as they do not have TestMethodCoverage 
            let coverageToCheck = testMethodCoverages[coverageIdToCheck].Value
            let sampleCoverage = testMethodCoverages[sampleCoverageId].Value
            let methodCount = sampleCoverage.Length
            // checking if a test is covering all spots as some other test
            // by iteratively going through each method called in the test project
            for methodId = 0 to methodCount - 1 do
                let mutable idCheck = 0
                let mutable idSample = 0
                // TODO: update with renewed types
                let checkMethodCoverage = coverageToCheck[methodId]
                let sampleMethodCoverage = sampleCoverage[methodId]
                let checkCoverageSize = checkMethodCoverage.Length
                let sampleCoverageSize = sampleMethodCoverage.Length
                
                // going through every coverage of both tests; offsets are sorted in ascending order
                while result && idCheck < checkCoverageSize && idSample < sampleCoverageSize do
                    let offsetCheck = checkMethodCoverage[idCheck]
                    let offsetSample = sampleMethodCoverage[idSample]
                    match result with
                    // the test being checked covered a block not covered in the test sample; returning false
                    | _ when offsetCheck < offsetSample -> result <- false
                    // block covered by the test being checked is present in the test sample
                    | _ when offsetCheck = offsetSample -> idCheck <- idCheck + 1
                    // test sample has extra covered blocks; skipping over them
                    | _ (* offsetCheck > offsetSample *) -> idSample <- idSample + 1
                    // less, equal and greater cover all branches for int comparison
                
                // checking the reason while loop stopped
                // idSample overflow means test being checked has a covered block, not covered by sample test
                // thus, setting the overlap result as false
                if idCheck < checkCoverageSize && idSample = sampleCoverageSize then result <- false
            result
            
        let testMethodsCount = testMethodCoverages.Length
        
        [| 0..testMethodsCount - 1 |]
        |> Array.map (fun id ->
            let mutable coveredBy = None
            
            let mutable i = id + 1
            while coveredBy.IsNone && i < testMethodsCount do
                if checkIfConsistsIn id i then
                    coveredBy <- Some i
                i <- i + 1
            
            i <- 0
            while coveredBy.IsNone && i < id do
                if checkIfConsistsIn id i then
                    coveredBy <- Some i
                i <- i + 1
                
            coveredBy
        )
        
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
    
    static let testAttributesFullNames = lazy(testAttributesList |> List.map (fun attr -> attr.FullName))
        
    static let isMethodATest (method: MethodBase) =
        method.CustomAttributes
        |> Seq.cast<CustomAttributeData>
        |> Seq.exists (fun x ->
            List.contains x.AttributeType.FullName testAttributesFullNames.Value
        )

    static let findDuplicateTests (assemblyPath, reports: RawCoverageReports) =
        let assembly = AssemblyManager.LoadFromAssemblyPath(assemblyPath)
        let methodsOfReports = reports.methods
        let methodsCount = methodsOfReports.Count
        let methodBases =
            (fun i -> lazy (Reflection.resolveMethodBaseFromAssembly assembly assemblyPath (int32 methodsOfReports[i].methodToken)))
            |> Array.init methodsCount
            
        let isTestMethodsById =
            (fun i -> lazy (isMethodATest methodBases[i].Value))
            |> Array.init methodsCount
            
        let composeReport (testCoveredBy: int option array) =
            let testCount = testCoveredBy.Length
            
            let mutable testsOverlapped = []
            let mutable testsCovering = []
            let mutable testsUnique = []
            let mutable identicalTestsGroups = []
            let isMarked = Array.create testCount false
            
            // TODO: add all cases for tests and fill other lists
            for i = 0 to testCount - 1 do
                if isMarked[i] |> not then
                    isMarked[i] <- true
                    if testCoveredBy[i].IsNone
                    then testsUnique <- testsUnique
                    
        let testIdToMethodId = Dictionary<int, int>()
        let mutable nextId = 0
        
        reports.reports
        // leaving only test calls in
        // coverage report always starts with EnterMain - enter to a main (in this case, test) method
        |> Array.filter (fun report -> isTestMethodsById[report.rawCoverageLocations[0].methodId].Value)
        |> recomposeTestMethodCoverage methodsCount
        // TODO: finish the pipe with renewed types
        |> findOverlappingCoverages
        |> Array.iter (fun x ->
            ()
        )
            
    static member FindRedundantTests(coverageReports: RawCoverageReport[]) =
        ()
