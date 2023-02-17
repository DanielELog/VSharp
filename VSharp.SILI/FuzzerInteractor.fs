namespace VSharp.Fuzzer

open System.Diagnostics
open System.IO
open System.IO.Pipes
open VSharp

type Message =
    | Kill
    | Fuzz of string * int

    static member serialize msg =
        match msg with
        | Kill -> "Kill"
        | Fuzz (moduleName, methodToken) -> $"Fuzz %s{moduleName} %d{methodToken}"

    static member deserialize (str: string) =
        let parts = str.Split [|' '|]
        match parts.[0] with
        | "Kill" -> Kill
        | "Fuzz" -> Fuzz (parts[1], int parts[2])
        | _ -> failwith "Unknown message"

type private FuzzerPipeClient () =
    let io = new NamedPipeClientStream(".", "FuzzerPipe", PipeDirection.Out)
    let writer = new StreamWriter(io)

    do
        io.Connect()
        printfn "Connected!"
        writer.AutoFlush <- true

    member this.SendMessage msg =
        writer.WriteAsync (Message.serialize msg)
        |> Async.AwaitTask

type FuzzerInteractor (pathToAssembly, outputDir) =
    let proc =
        let config =
            let info = ProcessStartInfo()
            info.FileName <- "dotnet"
            info.Arguments <- $"/home/viktor/RiderProjects/VSharp/VSharp.Fuzzer/bin/Debug/net6.0/VSharp.Fuzzer.dll %s{pathToAssembly} %s{outputDir}"
            info.UseShellExecute <- false
            info.RedirectStandardInput <- false
            info.RedirectStandardOutput <- false
            info.RedirectStandardError <- false
            info
        printfn "Started!"
        Process.Start(config)


    let client = FuzzerPipeClient()
    member this.Fuzz (moduleName: string, methodToken: int) = client.SendMessage (Fuzz (moduleName, methodToken))
    member this.Kill () = proc.Kill ()

