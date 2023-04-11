namespace VSharp.Fuzzer

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Runtime.InteropServices
open System.Text
open System.Threading
open Microsoft.FSharp.NativeInterop
open VSharp
open VSharp.Interpreter.IL

#nowarn "9"

type ClientMessage =
    | Kill
    | Fuzz of string * int

    static member serialize msg =
        match msg with
        | Kill -> "Kill"
        | Fuzz (moduleName, methodToken) -> $"Fuzz %s{moduleName} %d{methodToken}"

    static member deserialize (str: string) =
        let parts = str.Split ' '
        match parts[0] with
        | "Kill" -> Kill
        | "Fuzz" -> assert (parts.Length = 3); Fuzz (parts[1], int parts[2])
        | _ -> internalfail $"Unknown client message: {str}"

type ServerMessage =
    | Statistics of CoverageLocation array

    static member serialize msg =
        match msg with
        | Statistics arr ->
            let content = Array.map CoverageLocation.serialize arr |> String.concat ";"
            $"Statistics %s{content}"


    static member deserialize (str: string) =
        let parts = str.Split ' '
        match parts[0] with
        | "Statistics" ->
            assert (parts.Length = 2)
            let parts = parts[1].Split ';'
            let content = Array.map CoverageLocation.deserialize parts
            Statistics content
        | _  -> internalfail $"Unknown server message: {str}"

type private FuzzerPipe<'a, 'b> (io: Stream, onStart, serialize: 'a -> string, deserialize: string -> 'b) =
    let reader = new StreamReader(io)
    let writer = new StreamWriter(io)

    do
        onStart ()
        writer.AutoFlush <- true

    member this.ReadMessage () =
        async {
            let! str = reader.ReadLineAsync() |> Async.AwaitTask
            Logger.trace $"Received raw msg: {str}"
            return deserialize str
        }

    member this.ReadAll onEach =
        async {
            let mutable completed = false
            while not completed do
                let! str = reader.ReadLineAsync() |> Async.AwaitTask
                if str = null then
                    completed <- true
                else
                    let! stop = deserialize str |> onEach
                    completed <- stop
        }

    member this.SendMessage msg =
        writer.WriteAsync $"{serialize msg}\n" |> Async.AwaitTask

    member this.SendEnd () =
        writer.Close ()

module MeasureTime =
    let measureTime name f =
        let start = DateTime.Now
        let result = f ()
        let end_ = DateTime.Now
        let diff = start - end_
        Logger.error $"{name}: {diff.Milliseconds}"
        result


module InteropSyncCalls =
    [<DllImport("libvsharpConcolic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void SetEntryMain(byte* assemblyName, int assemblyNameLength, byte* moduleName, int moduleNameLength, int methodToken)

    [<DllImport("libvsharpConcolic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void GetHistory(nativeint size, nativeint data)

    let getHistory () =
        let sizePtr = NativePtr.stackalloc<uint> 1
        let dataPtrPtr = NativePtr.stackalloc<nativeint> 1
        Logger.error $"pointer before: {NativePtr.toNativeInt dataPtrPtr}"
        Logger.error $"value before: {NativePtr.read dataPtrPtr}"

        GetHistory(NativePtr.toNativeInt sizePtr, NativePtr.toNativeInt dataPtrPtr)

        let size = NativePtr.read sizePtr |> int
        let dataPtr = NativePtr.read dataPtrPtr

        let data = Array.create size (byte 0)

        Marshal.Copy(dataPtr, data, 0, size)

        let history = CoverageDeserializer.getHistory data
        MeasureTime.measureTime "free" <| fun () -> Marshal.FreeCoTaskMem(dataPtr)
        history

type FuzzerApplication (assembly, outputDir) =
    let fuzzer = Fuzzer ()
    let server =
        let io = new NamedPipeServerStream("FuzzerPipe", PipeDirection.InOut)
        FuzzerPipe (io, io.WaitForConnection, ServerMessage.serialize, ClientMessage.deserialize)

    let mutable freeId = -1
    let nextId () =
        freeId <- freeId + 1
        freeId

    let handleRequest command =
        async {
            match command with
            | Fuzz (moduleName, methodToken) ->
                let methodBase = Reflection.resolveMethodBaseFromAssembly assembly moduleName methodToken
                let method = Application.getMethod methodBase

                let assemblyNamePtr = fixed assembly.FullName.ToCharArray()
                let moduleNamePtr = fixed moduleName.ToCharArray()
                let assemblyNameLength = assembly.FullName.Length
                let moduleNameLength = moduleName.Length
                InteropSyncCalls.SetEntryMain(assemblyNamePtr |> NativePtr.toVoidPtr |> NativePtr.ofVoidPtr, assemblyNameLength, moduleNamePtr |> NativePtr.toVoidPtr |> NativePtr.ofVoidPtr, moduleNameLength, methodToken)

                Logger.error $"Start fuzzing {moduleName} {methodToken}"

                do! fuzzer.FuzzWithAction method (fun state -> async {
                    let test = TestGenerator.state2test false method state ""
                    match test with
                    | Some test ->
                        let filePath = $"{outputDir}{Path.DirectorySeparatorChar}fuzzer_test{nextId ()}.vst"
                        Logger.error $"Saved to {filePath}"
                        let hist = InteropSyncCalls.getHistory()
                        Logger.error $"count: {hist.Length}"
                        Logger.error $"size: {hist[0].Length}"
                        do! server.SendMessage (Statistics hist[0])
                        test.Serialize filePath
                    | None -> ()
                })

                Logger.error $"Successfully fuzzed {moduleName} {methodToken}"
                return false

            | Kill -> return true
        }

    member this.Start () = server.ReadAll handleRequest

type FuzzerInteraction (pathToAssembly, outputDir, cancellationToken: CancellationToken, saveStatistic: codeLocation seq -> unit) =
    // TODO: find correct path to the client
    let extension =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then ".dll"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then ".so"
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then ".dylib"
        else __notImplemented__()
    let pathToClient = $"libvsharpConcolic{extension}"
    let profiler = $"%s{Directory.GetCurrentDirectory()}%c{Path.DirectorySeparatorChar}%s{pathToClient}"

    let proc =
        let config =
            let info = ProcessStartInfo()
            info.EnvironmentVariables.["CORECLR_PROFILER"] <- "{2800fea6-9667-4b42-a2b6-45dc98e77e9e}"
            info.EnvironmentVariables.["CORECLR_ENABLE_PROFILING"] <- "1"
            info.EnvironmentVariables.["CORECLR_PROFILER_PATH"] <- profiler
            info.WorkingDirectory <- Directory.GetCurrentDirectory()
            info.FileName <- "dotnet"
            info.Arguments <- $"VSharp.Fuzzer.dll %s{pathToAssembly} %s{outputDir}"
            info.UseShellExecute <- false
            info.RedirectStandardInput <- false
            info.RedirectStandardOutput <- false
            info.RedirectStandardError <- false
            info
        Logger.trace "Fuzzer started"
        Process.Start(config)

    let client =
        let io = new NamedPipeClientStream(".", "FuzzerPipe", PipeDirection.InOut)
        FuzzerPipe(io, io.Connect, ClientMessage.serialize, ServerMessage.deserialize)

    let killFuzzer () = Logger.trace "Fuzzer killed"; proc.Kill ()

    let methods = System.Collections.Generic.Dictionary<int, Method>()
    let toSiliStatistic (loc: CoverageLocation seq) =

        let getMethod l =
            match methods.TryGetValue(l.methodToken) with
            | true, m -> m
            | false, _ ->
                let methodBase = Reflection.resolveMethodBase l.assemblyName l.moduleName l.methodToken
                let method = Method methodBase
                methods.Add (l.methodToken, method)
                method

        let toCodeLocation l =
            {
                offset = LanguagePrimitives.Int32WithMeasure l.offset
                method = getMethod l
            }

        loc |> Seq.map toCodeLocation

    let handleRequest msg =
        async {
            match msg with
            | Statistics s -> toSiliStatistic s |> saveStatistic
            return false
        }
    do
        cancellationToken.Register(killFuzzer)
        |> ignore

    member this.Fuzz (moduleName: string, methodToken: int) = client.SendMessage (Fuzz (moduleName, methodToken))

    member this.WaitStatistics ()  =
        async {
            do! client.SendMessage Kill
            Logger.error "Kill message sent to fuzzer"
            do! client.ReadAll handleRequest
            do! proc.WaitForExitAsync () |> Async.AwaitTask
            Logger.error "Fuzzer stopped"
        }

    member this.Kill = killFuzzer
