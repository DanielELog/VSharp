namespace VSharp.TestProcessing

open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Core
open VSharp

type SimplifiedLocation =
    {
        methodId: int32
        offset: uint32
    }
    
type AssemblyReport =
    {
        assembly: string
        reports: RawCoverageReports
        testMethods: string list
    }

type TestsCoverages = Dictionary<int, HashSet<SimplifiedLocation>>
    
type FullCoverage =
    {
        methodsCount: int32
        testCoverages: TestsCoverages
    }
    
type UniqueMethodIdentification =
    {
        token: int32
        asmName: string
    }

// first array symbolizes each method that was recorded in coverage
// second one - each offset covered by test in said method
type TestMethodCoverage = uint32 array array

type CrosscheckerReport =
    {
        assembly: string
        testsOverlapped: MethodBase[] // tests whose coverage is being covered by a bigger test
        testsCovering: MethodBase[] // bigger tests, covering the above
        testsUnique: MethodBase[] // tests that do not cover other, nor are covered by other
        testGroupsIdentical: MethodBase[][] // groups of tests with exactly the same coverage
    }
    
type MinimizerReport =
    {
        minimized: string seq
        unused: string seq
    }
    
// provides a Dictionary that can keep the first recorded value instance
type DictionaryNoOverwrite<'K, 'V when 'K: equality and 'V: equality>() =
    inherit Dictionary<'K, 'V>()
            
    member this.SetOrKeepAndGet(key: 'K, valueInit: unit -> 'V) =
        if this.ContainsKey(key) |> not
        then this[key] <- valueInit()
        this[key]

type MethodWriter private() =
    static member GetString(mBase: MethodBase) =
        $"{mBase} of {mBase.DeclaringType} in {mBase.Module.Assembly.FullName}"
