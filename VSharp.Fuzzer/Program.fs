module VSharp.Fuzzer.Program

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open VSharp.Fuzzer
open VSharp
open VSharp.Interpreter.IL
open VSharp.Reflection


let getAssembly argv =
    if Array.length argv < 1 then failwith "Unspecified path to assembly"
    let assemblyPath = argv[0]
    AssemblyManager.LoadFromAssemblyPath assemblyPath

let getOutputDir argv =
    if Array.length argv < 2 then failwith "Unspecified path to output directory"
    argv[1]

let makeCilState entryMethod state =
    let currentLoc = { offset = 0<offsets>; method = entryMethod }
    { ipStack = [Exit entryMethod]
      currentLoc = currentLoc
      state = state
      filterResult = None
      iie = None
      level = PersistentDict.empty
      startingIP = Instruction (0<offsets>, entryMethod)
      initialEvaluationStackSize = 0u
      stepsNumber = 0u
      suspended = false
      targets = None
      lastPushInfo = None
      history = Set.singleton currentLoc
      entryMethod = Some entryMethod
      id = 0u
    }

type FuzzerPipeServer () =
    let io = new NamedPipeServerStream("FuzzerPipe", PipeDirection.In)
    let reader = new StreamReader(io)

    do
        io.WaitForConnection()

    member this.ReadMessage () =
        async {
            let! str = reader.ReadLineAsync() |> Async.AwaitTask
            return Message.deserialize str
        }

type FuzzerApplication (assembly, outputDir) =
    let fuzzer = Fuzzer ()
    let server = FuzzerPipeServer ()
    member this.Start () =
        let loop () =
            async {
                let! command = server.ReadMessage()
                Logger.error $"Received {command}"
                match command with
                | Fuzz (moduleName, methodToken) ->
                    let methodBase = resolveMethodBaseFromAssembly assembly moduleName methodToken
                    let method = Application.getMethod methodBase
                    Logger.error "Try to fuzz"
                    let result = fuzzer.Fuzz method
                    Logger.error "Fuzzed"
                    result
                    |> Seq.map (fun x -> TestGenerator.state2test false method None (makeCilState method x) "")
                    |> Seq.iteri (fun i x ->
                        Logger.error $"Saved to {outputDir}{Path.DirectorySeparatorChar}fuzzer_test{i}.vst"
                        x.Value.Serialize $"{outputDir}{Path.DirectorySeparatorChar}fuzzer_test{i}.vst")
                | Kill -> ()
            }
        loop()

[<EntryPoint>]
let main argv =

    Console.SetOut (new StreamWriter (File.OpenWrite ("/home/viktor/Desktop/fuzzer.log")))
    Logger.error $"PID: {Process.GetCurrentProcess().Id}"
    Logger.error "Fuzzer started!"
    let assembly = getAssembly argv
    let outputDir = getOutputDir argv
    let app = FuzzerApplication (assembly, outputDir)
    app.Start() |> Async.RunSynchronously
    0
