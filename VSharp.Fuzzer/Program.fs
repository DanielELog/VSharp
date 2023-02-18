module VSharp.Fuzzer.Program

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open VSharp.Fuzzer
open VSharp
open VSharp.Interpreter.IL
open VSharp.Reflection
open System.Runtime.InteropServices
open FSharp.NativeInterop

module InteropSyncCalls =
        [<DllImport("libvsharpConcolic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
        extern void SyncInfoGettersPointers(Int64 instrumentPtr)

        [<DllImport("libvsharpConcolic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
        extern byte* GetProbes(uint* byteCount)

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

[<UnmanagedFunctionPointer(CallingConvention.StdCall)>]
type CallInstrumenterType =
    delegate of int -> int
        // token : uint *
        // codeSize : uint (assemblyNameLength : uint) (moduleNameLength : uint)
        //     (maxStackSize : uint) (ehsSize : uint) (signatureTokensLength : uint) (signatureTokensPtr : nativeptr<byte>) (assemblyNamePtr : nativeptr<char>)
        //     (moduleNamePtr : nativeptr<char>) (byteCodePtr : nativeptr<byte>) (ehsPtr : nativeptr<byte>)
        //     // result
        //     (instrumentedBody : nativeptr<nativeptr<byte>>) (length : nativeptr<int>) (resultMaxStackSize : nativeptr<int>)
        //     (resultEhs : nativeptr<nativeptr<byte>>) (ehsLength : nativeptr<int>)

let CallInstrumenter (a : int) =
    Logger.trace "hello from c++! %O" a
    1

let Invoker = CallInstrumenterType(CallInstrumenter)

type FuzzerPipeServer () =
    let io = new NamedPipeServerStream("FuzzerPipe", PipeDirection.In)
    let reader = new StreamReader(io)

    do
        io.WaitForConnection()

    member this.ReadMessage () =
        async {
            let! str = reader.ReadLineAsync() |> Async.AwaitTask
            Logger.error $"Recived raw msg: {str}"
            return Message.deserialize str
        }

type FuzzerApplication (assembly, outputDir) =
    let fuzzer = Fuzzer ()
    let server = FuzzerPipeServer ()
    member this.Start () =
        let fptr = Marshal.GetFunctionPointerForDelegate Invoker
        InteropSyncCalls.SyncInfoGettersPointers(fptr.ToInt64())
        let rec loop () =
            async {
                Logger.error $"Try to read message"
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
                        x.Value.Serialize $"{outputDir}{Path.DirectorySeparatorChar}fuzzer_test{i}.vst"
                    )
                    do! loop ()
                | Kill -> ()
            }
        Logger.error "Loop started"
        loop()

[<EntryPoint>]
let main argv =
    let assembly = getAssembly argv
    let outputDir = getOutputDir argv
    let logName = outputDir.Split Path.DirectorySeparatorChar |> Seq.last
    let out = new StreamWriter (File.OpenWrite ($"/home/daniel/work/FuzzerLogs/{logName}.log"))
    Console.SetOut out
    Console.SetError out
    Logger.error $"PID: {Process.GetCurrentProcess().Id}"
    Logger.error "Fuzzer started!"

    let app = FuzzerApplication (assembly, outputDir)
    Logger.error "App created"
    app.Start() |> Async.RunSynchronously
    0
