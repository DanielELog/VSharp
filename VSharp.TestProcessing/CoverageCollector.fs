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

open VSharp.CoverageTool

type CoverageCollector private() =
        
    static let getThreadedFileName (fileName: string) (extension: string) =
        $"{fileName}{Environment.CurrentManagedThreadId}.{extension}"
    static let coverageResultFileName() =
        getThreadedFileName "coverage" "cov"
    static let userAssembliesFileName() =
        getThreadedFileName "userAssemblies" "vsharp.log"
    static let testAssembliesFileName() =
        getThreadedFileName "testAssemblies" "vsharp.log"
    static let testOrderResultFileName() =
        getThreadedFileName "testOrder" "log"

    static let writeStringsToFile strings fileName =
        let fs = File.Create(fileName)
        fs.Write(UTF8Encoding(true).GetBytes(String.concat "\n" strings))
        fs.Close()
        fs.Name
        
    static let disposeUsedFiles() =
        File.Delete <| coverageResultFileName()
        File.Delete <| userAssembliesFileName()
        File.Delete <| testAssembliesFileName()
        
    static let readTestOrder() =
        try
            testOrderResultFileName()
            |> File.ReadLines
            |> List.ofSeq
            |> Some
        with :? FileNotFoundException ->
            None
            
    static member RetrieveAssemblyReport(coveragePath: string, testOrderPath: string) =
        [{
            assembly = ""
            reports = File.ReadAllBytes(coveragePath)
                  |> CoverageDeserializer.getRawReports
            testMethods =
                testOrderPath
                |> File.ReadLines
                |> List.ofSeq
        }]
        
    static member Collect(projectAssemblies: ProjectAssemblies) =
        let testDllsPath = writeStringsToFile projectAssemblies.testAssemblies <| testAssembliesFileName()
        let userDllsPath = writeStringsToFile projectAssemblies.userAssemblies <| userAssembliesFileName()
        let resultPath = writeStringsToFile Seq.empty <| coverageResultFileName()
        
        try
            let coverageTool = UnderDotnetTestCoverageTool(userDllsPath, testDllsPath, resultPath)
        
            projectAssemblies.testAssemblies
            |> List.map (fun x ->
                let threadedTestResultName = testOrderResultFileName()
                let filteredName = x.Split(Path.GetInvalidFileNameChars())
                let charSep = "_"
                let savedAssemblyCoverageName = $"coverage_{String.concat charSep filteredName}.cov"
                let cov = coverageTool.RunTestProjectWithCoverage(x, threadedTestResultName)
                
                // used as a cache for Coverage Explorer or TestProcessing to run without invoking `dotnet test` again
                File.Copy(resultPath, savedAssemblyCoverageName, true)
                
                // must be a better way to collect tests and put it inside of the AssemblyReport
                // adding it inside CoverageCollector is odd
                let testMethods = readTestOrder()
                if testMethods.IsNone then Logger.warning $"could not fetch test names from {x}! skipping assembly;"
                
                if File.Exists(threadedTestResultName) then File.Delete <| threadedTestResultName
                
                testMethods
                |> Option.map2 (fun y z ->
                    {
                        assembly = x
                        reports = y
                        testMethods = z
                    }) cov
            )
            |> List.choose id
        
        finally
            disposeUsedFiles()
