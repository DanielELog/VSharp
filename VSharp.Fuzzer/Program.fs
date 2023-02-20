module VSharp.Fuzzer.Program

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Text
open VSharp.Concolic
open VSharp.Fuzzer
open VSharp
open VSharp.Interpreter.IL
open VSharp.Reflection
open System.Runtime.InteropServices
open FSharp.NativeInterop

module InteropSyncCalls =
    [<DllImport(baka.pathToConcolic, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void SyncInfoGettersPointers(Int64 instrumentPtr)

    [<DllImport(baka.pathToConcolic, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
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
    // delegate of int -> int
    delegate of
        token : uint *
        codeSize : uint *
        assemblyNameLength : uint *
        moduleNameLength : uint *
        maxStackSize : uint *
        ehsSize : uint *
        signatureTokensLength : uint *
        signatureTokensPtr : nativeptr<byte> *
        assemblyNamePtr : nativeptr<char> *
        moduleNamePtr : nativeptr<char> *
        byteCodePtr : nativeptr<byte> *
        ehsPtr : nativeptr<byte> *
        // result
        instrumentedBody : nativeptr<nativeptr<byte>> *
        length : nativeptr<int> *
        resultMaxStackSize : nativeptr<int> *
        resultEhs : nativeptr<nativeptr<byte>> *
        ehsLength : nativeptr<int> -> unit

let instrumenter = InstrumenterCoverage

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

    static let mutable instrumenterCoverage : Option<InstrumenterCoverage> = None

    static member public CallInstrumenter (token : uint)
            (codeSize : uint)
            (assemblyNameLength : uint)
            (moduleNameLength : uint)
            (maxStackSize : uint)
            (ehsSize : uint)
            (signatureTokensLength : uint)
            (signatureTokensPtr : nativeptr<byte>)
            (assemblyNamePtr : nativeptr<char>)
            (moduleNamePtr : nativeptr<char>)
            (byteCodePtr : nativeptr<byte>)
            (ehsPtr : nativeptr<byte>)
            // result
            (instrumentedBody : nativeptr<nativeptr<byte>>)
            (length : nativeptr<int>)
            (resultMaxStackSize : nativeptr<int>)
            (resultEhs : nativeptr<nativeptr<byte>>)
            (ehsLength : nativeptr<int>) =
        // serialization
        let tokensLength = Marshal.SizeOf typeof<signatureTokens>
        let signatureTokensBytes : byte array = Array.zeroCreate tokensLength
        Marshal.Copy(NativePtr.toNativeInt signatureTokensPtr, signatureTokensBytes, 0, tokensLength)

        let tokens = Communicator.Deserialize<signatureTokens>(signatureTokensBytes)
        let assemblyNameLengthInt = assemblyNameLength |> int
        let assemblyNameBytes : byte array = Array.zeroCreate assemblyNameLengthInt
        Marshal.Copy(NativePtr.toNativeInt assemblyNamePtr, assemblyNameBytes, 0, assemblyNameLengthInt)

        let assembly = Encoding.Unicode.GetString(assemblyNameBytes)
        let moduleNameLengthInt = moduleNameLength |> int
        let moduleNameBytes : byte array = Array.zeroCreate moduleNameLengthInt
        Marshal.Copy(NativePtr.toNativeInt moduleNamePtr, moduleNameBytes, 0, moduleNameLengthInt)

        let methodModule = Encoding.Unicode.GetString(moduleNameBytes)
        let codeSizeInt = codeSize |> int
        let codeBytes : byte array = Array.zeroCreate codeSizeInt
        Marshal.Copy(NativePtr.toNativeInt byteCodePtr, codeBytes, 0, codeSizeInt)

        let ehSize = Marshal.SizeOf typeof<rawExceptionHandler>
        let count = (ehsSize |> int) / ehSize
        let ehsSizeInt = ehsSize |> int
        let ehsBytes : byte array = Array.zeroCreate ehsSizeInt
        let ehs : rawExceptionHandler array =
            if count > 0 then
                assert(NativePtr.isNullPtr ehsPtr |> not)
                Marshal.Copy(NativePtr.toNativeInt ehsPtr, ehsBytes, 0, ehsSizeInt)
                [| for i in 0 .. count - 1 -> Communicator.Deserialize<rawExceptionHandler>(ehsBytes, i * ehSize) |]
            else
                Array.zeroCreate 0

        // instrumentation
        let properties : rawMethodProperties =
            { token = token; ilCodeSize = codeSize; assemblyNameLength = assemblyNameLength; moduleNameLength = moduleNameLength; maxStackSize = maxStackSize; signatureTokensLength = signatureTokensLength }
        let methodBody : rawMethodBody =
            {properties = properties; assembly = assembly; tokens = tokens; moduleName = methodModule; il = codeBytes; ehs = ehs}

        let instrumented =
            match instrumenterCoverage with
            | Some instr -> instr.Instrument(methodBody)
            | None -> internalfailf "requesting intstrumenter before instantiation!"

        // result deserialization
        NativePtr.write instrumentedBody (Marshal.UnsafeAddrOfPinnedArrayElement(instrumented.il, 0) |> NativePtr.ofNativeInt)
        NativePtr.write length (instrumented.properties.ilCodeSize |> int)
        NativePtr.write resultMaxStackSize (instrumented.properties.maxStackSize |> int)
        let instrumentedEhsLength = instrumented.ehs.Length
        let ehBytes : byte array = Array.zeroCreate (ehSize * instrumentedEhsLength)
        for i in 0 .. instrumentedEhsLength - 1 do
            Communicator.Serialize(instrumented.ehs[i], ehBytes, i * ehSize)
        NativePtr.write resultEhs (Marshal.UnsafeAddrOfPinnedArrayElement(ehBytes, 0) |> NativePtr.ofNativeInt)
        NativePtr.write ehsLength ehBytes.Length
        ()

    member this.Start () =
        let rec loop () =
            async {
                Logger.error $"Try to read message"
                let! command = server.ReadMessage()
                Logger.error $"Received {command}"
                match command with
                | Fuzz (moduleName, methodToken) ->
                    let methodBase = resolveMethodBaseFromAssembly assembly moduleName methodToken
                    let method = Application.getMethod methodBase

                    // setting up instrumenter for coverage profiler
                    let mutable bytesCount : uint = 0u
                    let probesPtr = InteropSyncCalls.GetProbes(&&bytesCount)
                    let probesBytes : byte array = Array.zeroCreate (bytesCount |> int)
                    Marshal.Copy(NativePtr.toNativeInt probesPtr, probesBytes, 0, bytesCount |> int)
                    let probes = Communicator.Deserialize<probesCov>(probesBytes)
                    instrumenterCoverage <- InstrumenterCoverage(methodBase, probes) |> Some

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
    let out = new StreamWriter (File.OpenWrite ($"/home/daniel/work/FuzzerLogs/kek.log"))
    Console.SetOut out
    Console.SetError out
    Logger.error "started"
    let Invoker = CallInstrumenterType(FuzzerApplication.CallInstrumenter)
    let fptr = Marshal.GetFunctionPointerForDelegate Invoker
    InteropSyncCalls.SyncInfoGettersPointers(fptr.ToInt64())
    Logger.error "sent pointers"
    let assembly = getAssembly argv
    let outputDir = getOutputDir argv
    let logName = outputDir.Split Path.DirectorySeparatorChar |> Seq.last

    Logger.error $"PID: {Process.GetCurrentProcess().Id}"
    Logger.error "Fuzzer started!"

    let app = FuzzerApplication (assembly, outputDir)
    Logger.error "App created"
    app.Start() |> Async.RunSynchronously
    0
