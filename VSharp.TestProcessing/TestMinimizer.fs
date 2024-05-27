namespace VSharp.TestProcessing

open System.Collections.Generic
open System.Linq
open VSharp

type private MinimizerLogic(
        testsOnLocation: Dictionary<SimplifiedLocation, List<int>>,
        methodAddedCoverage: Dictionary<int, int>,
        locsOfTest: Dictionary<int, HashSet<SimplifiedLocation>>,
        chooser: IReadOnlyDictionary<SimplifiedLocation, List<int>> -> Dictionary<int, int> -> int option
    ) =
    
    let testsTaken = List<int>()
    
    let coverLoc loc =
        if testsOnLocation.ContainsKey(loc) |> not
        then ()
        else
            testsOnLocation[loc]
            |> Seq.iter (fun id -> methodAddedCoverage[id] <- methodAddedCoverage[id] - 1)
            testsOnLocation.Remove(loc) |> ignore
    
    let takeTest testId =
        testsTaken.Add(testId)
        locsOfTest[testId]
        |> Seq.iter coverLoc
        
    let chooseNext() =
        chooser testsOnLocation methodAddedCoverage
        
    static member BestFitChooser (testsOnLoc: IReadOnlyDictionary<SimplifiedLocation, List<int>>) (methodCoverages: Dictionary<int, int>) =
        let bestFit = methodCoverages
                    |> Seq.maxBy (fun kvp -> kvp.Value)
        if bestFit.Value = 0
        then None
        else Some bestFit.Key
        
    static member FirstFitChooser (testsOnLoc: IReadOnlyDictionary<SimplifiedLocation, List<int>>) (methodCoverages: Dictionary<int, int>) =
        if testsOnLoc.Count = 0
        then None
        else (Seq.head testsOnLoc).Value
             |> Seq.maxBy (fun id -> methodCoverages[id])
             |> Some    
        
    member this.Minimize() =
        // taking tests covering a location, not covered by any other
        testsOnLocation.Values
        |> Seq.filter (fun tests -> tests.Count = 1)
        |> Seq.iter (fun tests -> takeTest tests[0])
        
        let mutable nextTest = chooseNext()
        while nextTest.IsSome do
            takeTest nextTest.Value
            nextTest <- chooseNext()
            
        testsTaken

type private TestMinimizer private () =
    
    static let recomposeByLocation (tests: TestsCoverages) =
        let testsOnLocation = DictionaryNoOverwrite<SimplifiedLocation, List<int>>()
    
        let recordTestLocations testId locs =
            let testId = testId
            locs
            |> Seq.iter (fun loc ->
                let locList = testsOnLocation.SetOrKeepAndGet(loc, List<int>)
                locList.Add(testId)
            )

        tests
        |> Seq.iter (fun kvp -> recordTestLocations kvp.Key kvp.Value)
        
        testsOnLocation
        
    static let recomposeTestToCount (coverage: FullCoverage) =
        let countByMethod = Dictionary<int, int>()
        
        coverage.testCoverages
        |> Seq.iter (fun kvp ->
            countByMethod[kvp.Key] <- kvp.Value.Count
        )
        
        countByMethod
    
    static member Minimize(coverage: FullCoverage) =
        if coverage.testCoverages.Count = 0
        then
            Logger.warning $"TestMinimizer was given a coverage consisting of 0 tests! Returning empty sequence..."
            Seq.empty
        else
            MinimizerLogic(
                recomposeByLocation coverage.testCoverages,
                recomposeTestToCount coverage,
                coverage.testCoverages,
                MinimizerLogic.BestFitChooser
            ).Minimize()
            |> Seq.cast<int>
