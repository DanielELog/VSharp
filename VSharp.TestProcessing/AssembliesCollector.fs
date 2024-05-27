namespace VSharp.TestProcessing

open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open FSharpx.Collections
open VSharp.CSharpUtils

open VSharp

type ProjectAssemblies = {
    testAssemblies: string list
    userAssemblies: string list
}

type AssembliesCollector private () =
    static let getDotnetTestOutput (procInfo: ProcessStartInfo) =
        let mutable result = ""
        
        let proc = procInfo.StartWithLogging(
            (fun x -> result <- String.concat "\n" [result; x]),
            ignore
        )
        proc.WaitForExit()
        
        if proc.ExitCode <> 0
        then
            Logger.error "`dotnet test -t` failed to execute!"
            None
        else
            Some result
            
    static let createAssemblyCollectingProcInfo (projectRootDirectory: DirectoryInfo) =
        ProcessStartInfo()
        |> (fun x ->
                x.Arguments <- "test -t -c Release"
                x.FileName <- DotnetExecutablePath.ExecutablePath
                x.WorkingDirectory <- projectRootDirectory.FullName
                x
        )
        
    (* NOTE: when running `dotnet test` across the whole solution, test cases may be written simultaneously
             using project-only output will ensure where each test belongs to
    *)
    static let createTestCollectingProcInfo (testProjectPath: string) =
        ProcessStartInfo()
        |> (fun x ->
                x.Arguments <- $"test {testProjectPath} -t -c Release"
                x.FileName <- DotnetExecutablePath.ExecutablePath
                x
        )
            
    static let fetchLibrariesFromTestOutput output =
        // TODO: unix-compatible regex
        let sep = Path.DirectorySeparatorChar
        
        // catching all user dlls full paths from 'dotnet test -t' output with regex
        
        // NOTE: this relies on the output pattern of dotnet test, under .NET7.0
        // shall it change - the following logic must be adjusted accordingly
        
        // the match will look like " -> MyCool.dll" for all the user libraries
        // and " MyCoolTest.dll" for libraries representing test projects
        
        // the zeroth group represents whole captured substring
        
        // full path of captured dll in the second ([anything but \n]*.dll)
        
        // test libraries are mentioned twice - during restore, and during test execution
        let matchDll = @$"[\s^\n](->\s)?([^\s]*\{sep}([^\{sep}\n]+).dll)"
        
        let foundMatches = Regex.Matches(output, matchDll)
            
        let testDllsPaths =
            foundMatches
            |> Seq.filter (fun matched ->
                    // means there is no first group (-> ) which indicates a test dll
                    matched.Groups[1].Length = 0
                )
            // retrieves dll name itself
            |> Seq.map (fun x -> x.Groups[2].Captures[0].Value)
            |> Seq.filter (fun x -> x <> typeof<VSharp.TestProcessing.AssemblyReport>.Assembly.Location)
            |> Seq.toList
            
        // dotnet test restores test assemblies as well, so they'll be included here
        let userDllsPaths =
            foundMatches
            |> Seq.filter (fun matched ->
                    // means there is the first group (-> ) which was retrieved by restore
                    matched.Groups[1].Length <> 0
                )
            // retrieves dll name itself
            |> Seq.map (fun x -> x.Groups[3].Captures[0].Value)
            // TODO: special flag when intended to run against V#?
            // used to ignore custom runners themselves, which are used for test names collection
            |> Seq.filter (fun x -> x <> "NUnitTestRunner")
            |> Seq.toList
            
        {
            testAssemblies = testDllsPaths
            userAssemblies = userDllsPaths
        }
    
    static let TestEnumerationStart = "The following Tests are available:"
        
    static let fetchTestMethodsFromTestOutput output =
        let reader = new StringReader(output)
        let mutable enumerationStarted = false
        let mutable result = List.Empty
        
        let mutable line = reader.ReadLine()
        while line <> null && enumerationStarted |> not do
            enumerationStarted <- line = TestEnumerationStart
            line <- reader.ReadLine()
        while line <> null do
            result <- line :: result
            line <- reader.ReadLine()
            
        result
        
    static member GetTestAndUserLibraries(projectRootDirectory: DirectoryInfo) =
        createAssemblyCollectingProcInfo projectRootDirectory
        |> getDotnetTestOutput
        |> Option.map fetchLibrariesFromTestOutput
