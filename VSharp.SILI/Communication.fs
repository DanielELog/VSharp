namespace VSharp.Concolic

open System
open System.IO
open System.IO.Pipes
open System.Text
open System.Runtime.InteropServices
open VSharp
open VSharp.Core.API

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type probes = {
    mutable trackCoverage : uint64
    mutable enableInstrumentation : uint64
    mutable disableInstrumentation : uint64
    mutable ldarg_0 : uint64
    mutable ldarg_1 : uint64
    mutable ldarg_2 : uint64
    mutable ldarg_3 : uint64
    mutable ldarg_S : uint64
    mutable ldarg : uint64
    mutable ldarga_primitive : uint64
    mutable ldarga_struct : uint64

    mutable ldloc_0 : uint64
    mutable ldloc_1 : uint64
    mutable ldloc_2 : uint64
    mutable ldloc_3 : uint64
    mutable ldloc_S : uint64
    mutable ldloc : uint64
    mutable ldloca_primitive : uint64
    mutable ldloca_struct : uint64

    mutable starg_S : uint64
    mutable starg : uint64
    mutable stloc_0 : uint64
    mutable stloc_1 : uint64
    mutable stloc_2 : uint64
    mutable stloc_3 : uint64
    mutable stloc_S : uint64
    mutable stloc : uint64

    mutable ldc : uint64
    mutable dup : uint64
    mutable pop : uint64

    mutable brtrue : uint64
    mutable brfalse : uint64
    mutable switch : uint64

    mutable unOp : uint64
    mutable binOp : uint64
    mutable execBinOp_4 : uint64
    mutable execBinOp_8 : uint64
    mutable execBinOp_f4 : uint64
    mutable execBinOp_f8 : uint64
    mutable execBinOp_p : uint64
    mutable execBinOp_8_4 : uint64
    mutable execBinOp_4_p : uint64
    mutable execBinOp_p_4 : uint64
    mutable execBinOp_4_ovf : uint64
    mutable execBinOp_8_ovf : uint64
    mutable execBinOp_f4_ovf : uint64
    mutable execBinOp_f8_ovf : uint64
    mutable execBinOp_p_ovf : uint64
    mutable execBinOp_8_4_ovf : uint64
    mutable execBinOp_4_p_ovf : uint64
    mutable execBinOp_p_4_ovf : uint64

    mutable ldind : uint64
    mutable stind : uint64
    mutable execStind_I1 : uint64
    mutable execStind_I2 : uint64
    mutable execStind_I4 : uint64
    mutable execStind_I8 : uint64
    mutable execStind_R4 : uint64
    mutable execStind_R8 : uint64
    mutable execStind_ref : uint64

    mutable conv : uint64
    mutable conv_Ovf : uint64

    mutable newarr : uint64
    mutable localloc : uint64
    mutable ldobj : uint64
    mutable ldstr : uint64
    mutable ldtoken : uint64
    mutable stobj : uint64
    mutable initobj : uint64
    mutable ldlen : uint64

    mutable cpobj : uint64
    mutable execCpobj : uint64
    mutable cpblk : uint64
    mutable execCpblk : uint64
    mutable initblk : uint64
    mutable execInitblk : uint64

    mutable castclass : uint64
    mutable isinst : uint64

    mutable box : uint64
    mutable unbox : uint64
    mutable unboxAny : uint64

    mutable ldfld : uint64
    mutable ldfld_struct : uint64
    mutable ldflda : uint64
    mutable stfld_4 : uint64
    mutable stfld_8 : uint64
    mutable stfld_f4 : uint64
    mutable stfld_f8 : uint64
    mutable stfld_p : uint64
    mutable stfld_struct : uint64
    mutable stfld_refLikeStruct : uint64

    mutable ldsfld : uint64
    mutable ldsflda : uint64
    mutable stsfld : uint64

    mutable ldelema : uint64
    mutable ldelem : uint64
    mutable execLdelema : uint64
    mutable execLdelem : uint64

    mutable stelem : uint64
    mutable execStelem_I : uint64
    mutable execStelem_I1 : uint64
    mutable execStelem_I2 : uint64
    mutable execStelem_I4 : uint64
    mutable execStelem_I8 : uint64
    mutable execStelem_R4 : uint64
    mutable execStelem_R8 : uint64
    mutable execStelem_Ref : uint64
    mutable execStelem_Struct : uint64

    mutable ckfinite : uint64
    mutable sizeof : uint64
    mutable ldftn : uint64
    mutable ldvirtftn : uint64
    mutable arglist : uint64
    mutable mkrefany : uint64

    mutable setArgSize : uint64
    mutable setLocSize : uint64
    mutable enter : uint64
    mutable enterStructCtor : uint64
    mutable enterVirtual : uint64
    mutable enterMain : uint64
    mutable leave : uint64
    mutable leaveMain_0 : uint64
    mutable leaveMain_4 : uint64
    mutable leaveMain_8 : uint64
    mutable leaveMain_f4 : uint64
    mutable leaveMain_f8 : uint64
    mutable leaveMain_p : uint64
    mutable finalizeCall : uint64
    mutable execCall : uint64
    mutable execThisCall : uint64
    mutable execInternalCall : uint64
    mutable call : uint64
    mutable pushFrame : uint64
    mutable pushTemporaryAllocatedStruct : uint64
    mutable pushInternalCallResult : uint64
    mutable callVirt : uint64
    mutable newobj : uint64
    mutable calli : uint64
    mutable throw : uint64
    mutable rethrow : uint64

    mutable mem_1 : uint64
    mutable mem_2 : uint64
    mutable mem_4 : uint64
    mutable mem_8 : uint64
    mutable mem_f4 : uint64
    mutable mem_f8 : uint64
    mutable mem_p : uint64
    mutable mem2_4 : uint64
    mutable mem2_8 : uint64
    mutable mem2_f4 : uint64
    mutable mem2_f8 : uint64
    mutable mem2_8_4 : uint64
    mutable mem_refLikeStruct : uint64
    mutable unmem_1 : uint64
    mutable unmem_2 : uint64
    mutable unmem_4 : uint64
    mutable unmem_8 : uint64
    mutable unmem_f4 : uint64
    mutable unmem_f8 : uint64
    mutable unmem_p : uint64
    mutable popOpmem : uint64

    mutable dumpInstruction : uint64
}
with
    member private x.Probe2str =
        let map = System.Collections.Generic.Dictionary<uint64, string>()
        typeof<probes>.GetFields() |> Seq.iter (fun fld -> map.Add(fld.GetValue x |> unbox, fld.Name))
        map
    member x.AddressToString (address : int64) =
        let result = ref ""
        if x.Probe2str.TryGetValue(uint64 address, result) then "probe_" + !result
        else toString address

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type signatureTokens = {
    mutable void_sig : uint32
    mutable bool_sig : uint32
    mutable i_sig : uint32
    mutable void_u1_sig : uint32
    mutable void_u4_sig : uint32
    mutable void_i_sig : uint32
    mutable bool_i_sig : uint32
    mutable bool_u2_sig : uint32
    mutable i1_i1_sig : uint32
    mutable i2_i1_sig : uint32
    mutable i4_i1_sig : uint32
    mutable i8_i1_sig : uint32
    mutable r4_i1_sig : uint32
    mutable r8_i1_sig : uint32
    mutable i_i1_sig : uint32
    mutable void_i_i1_sig : uint32
    mutable void_i_i2_sig : uint32
    mutable void_i_u2_sig : uint32
    mutable void_i_i4_sig : uint32
    mutable void_i_i8_sig : uint32
    mutable void_i_r4_sig : uint32
    mutable void_i_r8_sig : uint32
    mutable void_i_i_sig : uint32
    mutable void_i1_size_sig : uint32
    mutable bool_i_i4_sig : uint32
    mutable bool_i_i_sig : uint32
    mutable bool_i_i_i4_sig : uint32
    mutable void_i_i_i_sig : uint32
    mutable void_i_i_i1_sig : uint32
    mutable void_i_i_i2_sig : uint32
    mutable void_i_i_i4_sig : uint32
    mutable void_i_i_i8_sig : uint32
    mutable void_i_i_r4_sig : uint32
    mutable void_i_i_r8_sig : uint32
    mutable void_i1_i1_offset_sig : uint32
    mutable void_i2_i1_offset_sig : uint32
    mutable void_i4_i1_offset_sig : uint32
    mutable void_i8_i1_offset_sig : uint32
    mutable void_r4_i1_offset_sig : uint32
    mutable void_r8_i1_offset_sig : uint32
    mutable void_i8_i4_offset_sig : uint32
    mutable void_i8_i8_offset_sig : uint32
    mutable void_r4_r4_offset_sig : uint32
    mutable void_r8_r8_offset_sig : uint32
    mutable void_i_i1_i1_sig : uint32
    mutable void_i_i4_i2_sig : uint32
    mutable void_offset_sig : uint32
    mutable void_u1_offset_sig : uint32
    mutable void_u2_offset_sig : uint32
    mutable void_i4_offset_sig : uint32
    mutable void_i8_offset_sig : uint32
    mutable void_r4_offset_sig : uint32
    mutable void_r8_offset_sig : uint32
    mutable void_i_offset_sig : uint32
    mutable void_token_offset_sig : uint32
    mutable void_size_offset_sig : uint32
    mutable void_i4_i4_offset_sig : uint32
    mutable void_i_i1_offset_sig : uint32
    mutable void_i_i2_offset_sig : uint32
    mutable void_i_i4_offset_sig : uint32
    mutable void_i_i8_offset_sig : uint32
    mutable void_i_r4_offset_sig : uint32
    mutable void_i_r8_offset_sig : uint32
    mutable void_i_i_offset_sig : uint32
    mutable void_i_token_offset_sig : uint32
    mutable void_i_u2_size_sig : uint32
    mutable void_i_i4_i4_offset_sig : uint32
    mutable void_u2_i4_i4_offset_sig : uint32
    mutable void_u2_i4_i_offset_sig : uint32
    mutable void_u2_i8_i4_offset_sig : uint32
    mutable void_u2_i8_i8_offset_sig : uint32
    mutable void_u2_r4_r4_offset_sig : uint32
    mutable void_u2_r8_r8_offset_sig : uint32
    mutable void_u2_i_i_offset_sig : uint32
    mutable void_u2_i_i4_offset_sig : uint32
    mutable void_i_i_i_offset_sig : uint32
    mutable void_i_i_i1_offset_sig : uint32
    mutable void_i_i_i2_offset_sig : uint32
    mutable void_i_i_i4_offset_sig : uint32
    mutable void_i_i_i8_offset_sig : uint32
    mutable void_i_i_r4_offset_sig : uint32
    mutable void_i_i_r8_offset_sig : uint32
    mutable void_i_i1_i_offset_sig : uint32
    mutable void_i4_i_i4_offset_sig : uint32
    mutable void_i4_i_i8_offset_sig : uint32
    mutable void_i4_i_r4_offset_sig : uint32
    mutable void_i4_i_r8_offset_sig : uint32
    mutable void_i4_i_i_offset_sig : uint32
    mutable void_token_i_i_offset_sig : uint32
    mutable void_i4_i4_i_i_offset_sig : uint32
    mutable void_i_i4_i_i_offset_sig : uint32
    mutable void_token_token_bool_u2_offset_sig : uint32
    mutable void_token_u4_u4_u4_u4_i1_sig : uint32
    mutable void_token_u4_u2_bool_u4_u4_sig : uint32
}
with
    member private x.SigToken2str =
        let map = System.Collections.Generic.Dictionary<uint32, string>()
        typeof<signatureTokens>.GetFields() |> Seq.iter (fun fld ->
            let token : uint32 = fld.GetValue x |> unbox
            if not <| map.ContainsKey token then map.Add(token, fld.Name))
        map
    member x.TokenToString (token : int32) =
        let result = ref ""
        if x.SigToken2str.TryGetValue(uint32 token, result) then !result
        else "<UNKNOWN TOKEN!>"

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type rawMethodProperties = {
    mutable token : uint32
    mutable ilCodeSize : uint32
    mutable assemblyNameLength : uint32
    mutable moduleNameLength : uint32
    mutable maxStackSize : uint32
    mutable signatureTokensLength : uint32
}

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type instrumentedMethodProperties = {
    mutable ilCodeSize : uint32
    mutable maxStackSize : uint32
}

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type rawExceptionHandler = {
    mutable flags : int
    mutable tryOffset : uint32
    mutable tryLength : uint32
    mutable handlerOffset : uint32
    mutable handlerLength : uint32
    mutable matcher : uint32
}

type rawMethodBody = {
    properties : rawMethodProperties
    assembly : string
    moduleName : string
    tokens : signatureTokens
    il : byte array
    ehs : rawExceptionHandler array
}

type instrumentedMethodBody = {
    properties : instrumentedMethodProperties
    il : byte array
    ehs : rawExceptionHandler array
}

// Lightweight types for representing coverage information
[<Struct>]
[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type coverageLocation = {
    moduleToken : int
    methodToken : int
    offset : int
    threadToken : int
    stackPush : byte // 0 = no push, 1 = symbolic push, 2 = concrete push

}
with
    override x.ToString() =
        sprintf "%x::%d[%O]" x.methodToken x.offset x.stackPush

type CorElementType =
    | ELEMENT_TYPE_END            = 0x0uy
    | ELEMENT_TYPE_VOID           = 0x1uy
    | ELEMENT_TYPE_BOOLEAN        = 0x2uy
    | ELEMENT_TYPE_CHAR           = 0x3uy
    | ELEMENT_TYPE_I1             = 0x4uy
    | ELEMENT_TYPE_U1             = 0x5uy
    | ELEMENT_TYPE_I2             = 0x6uy
    | ELEMENT_TYPE_U2             = 0x7uy
    | ELEMENT_TYPE_I4             = 0x8uy
    | ELEMENT_TYPE_U4             = 0x9uy
    | ELEMENT_TYPE_I8             = 0xauy
    | ELEMENT_TYPE_U8             = 0xbuy
    | ELEMENT_TYPE_R4             = 0xcuy
    | ELEMENT_TYPE_R8             = 0xduy
    | ELEMENT_TYPE_STRING         = 0xeuy

    | ELEMENT_TYPE_PTR            = 0xfuy
    | ELEMENT_TYPE_BYREF          = 0x10uy

    | ELEMENT_TYPE_VALUETYPE      = 0x11uy
    | ELEMENT_TYPE_CLASS          = 0x12uy
    | ELEMENT_TYPE_VAR            = 0x13uy
    | ELEMENT_TYPE_ARRAY          = 0x14uy
    | ELEMENT_TYPE_GENERICINST    = 0x15uy
    | ELEMENT_TYPE_TYPEDBYREF     = 0x16uy

    | ELEMENT_TYPE_I              = 0x18uy
    | ELEMENT_TYPE_U              = 0x19uy
    | ELEMENT_TYPE_FNPTR          = 0x1Buy
    | ELEMENT_TYPE_OBJECT         = 0x1Cuy
    | ELEMENT_TYPE_SZARRAY        = 0x1Duy
    | ELEMENT_TYPE_MVAR           = 0x1euy

    | ELEMENT_TYPE_CMOD_REQD      = 0x1Fuy
    | ELEMENT_TYPE_CMOD_OPT       = 0x20uy

    | ELEMENT_TYPE_INTERNAL       = 0x21uy
    | ELEMENT_TYPE_MAX            = 0x22uy

    | ELEMENT_TYPE_MODIFIER       = 0x40uy
    | ELEMENT_TYPE_SENTINEL       = 0x41uy
    | ELEMENT_TYPE_PINNED         = 0x45uy

type evalStackArgType =
    | OpSymbolic = 1
    | OpI4 = 2
    | OpI8 = 3
    | OpR4 = 4
    | OpR8 = 5
    | OpRef = 6
    | OpStruct = 7
    | OpEmpty = 8

type concolicAddressKey =
    | ReferenceType
    | LocalVariable of byte * byte // frame number * idx
    | Parameter of byte * byte // frame number * idx
    | Statics of int16 // static field id
    | TemporaryAllocatedStruct of byte * byte // frame number * offset

type concolicAddress = UIntPtr * UIntPtr * concolicAddressKey

type evalStackOperand =
    | EmptyOp
    | NumericOp of evalStackArgType * int64
    | PointerOp of concolicAddress

type concolicExceptionRegister =
    | UnhandledConcolic of UIntPtr * bool
    | CaughtConcolic of UIntPtr * bool
    | NoExceptionConcolic

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type private execCommandStatic = {
    isBranch : uint32
    newCallStackFramesCount : uint32
    ipStackCount : uint32
    callStackFramesPops : uint32
    evaluationStackPushesCount : uint32
    evaluationStackPops : uint32
    newAddressesCount : uint32
    deletedAddressesCount : uint32
    delegatesCount : uint32
    exceptionKind : byte
    exceptionRegister : UIntPtr
    exceptionIsConcrete : byte
    isTerminatedByException : byte
}
type execCommand = {
    isBranch : uint32
    callStackFramesPops : uint32
    evaluationStackPops : uint32
    exceptionRegister : concolicExceptionRegister
    isTerminatedByException : bool
    newCallStackFrames : array<int32 * int32>
    thisAddresses : concolicAddress array
    ipStack : int32 list
    evaluationStackPushes : evalStackOperand array // NOTE: operands for executing instruction
    newAddresses : UIntPtr array
    deletedAddresses : UIntPtr array
    delegateCoupling : array<UIntPtr * UIntPtr * UIntPtr>
    newAddressesTypes : Type array
    // TODO: add deleted addresses
    newCoveragePath : coverageLocation list
}

[<type: StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)>]
type execResponseStaticPart = {
    framesCount : uint32
    lastPush : byte
    opsLength : int // -1 if operands were not concretized, length otherwise
    hasResult : byte
}

type commandFromConcolic =
    | Instrument of rawMethodBody
    | ExecuteInstruction of execCommand
    | Terminate

type commandForConcolic =
    | ReadMethodBody
    | ReadString
    | ParseTypesInfoFromMethod
    | ParseTypeRef
    | ParseTypeSpec
    | ReadHeapBytes
    | ReadExecResponse
    | Unmarshall
    | UnmarshallArray
    | ReadWholeObject
    | ReadArray
    | ParseFieldRefTypeToken
    | ParseFieldDefTypeToken
    | ParseArgTypeToken
    | ParseLocalTypeToken
    | ParseReturnTypeToken
    | ParseDeclaringTypeToken

type Communicator(pipeFile) =

    let confirmationByte = byte(0x55)
    let instrumentCommandByte = byte(0x56)
    let executeCommandByte = byte(0x57)
    let readMethodBodyByte = byte(0x58)
    let readStringByte = byte(0x59)
    let parseTypesInfoByte = byte(0x60)
    let parseTypeRefByte = byte(0x61)
    let parseTypeSpecByte = byte(0x62)
    let readHeapBytesByte = byte(0x63)
    let readExecResponseByte = byte(0x64)
    let unmarshallByte = byte(0x65)
    let unmarshallArrayByte = byte(0x66)
    let readWholeObjectByte = byte(0x67)
    let readArrayByte = byte(0x68)
    let parseFieldRefTypeTokenByte = byte(0x69)
    let parseFieldDefTypeTokenByte = byte(0x70)
    let parseArgTypeTokenByte = byte(0x71)
    let parseLocalTypeTokenByte = byte(0x72)
    let parseReturnTypeTokenByte = byte(0x73)
    let parseDeclaringTypeTokenByte = byte(0x74)

    let confirmation = Array.singleton confirmationByte

    let server = new NamedPipeServerStream(pipeFile, PipeDirection.InOut)
    let stream = server :> Stream

    let reportError (exn : IOException) =
        Logger.error "Error occured during communication with the concolic client! Message: %s" exn.Message
        false

    let fail format = Printf.ksprintf (fun (s : string) -> raise <| IOException s) format

    let unexpectedlyTerminated() = fail "Communication with CLR: interaction unexpectedly terminated"

    let readConfirmation () =
        let buffer : byte[] = Array.zeroCreate 1
        let bytesRead = stream.Read(buffer, 0, 1)
        if bytesRead <> 1 || buffer.[0] <> confirmationByte then
            fail "Communication with CLR: could not get the confirmation message. Instead read %d bytes with message [%s]" bytesRead (Array.map toString buffer |> join " ")

    let writeConfirmation () =
        stream.Write(confirmation, 0, 1)

    let readCount () =
        let countBytes : byte[] = Array.zeroCreate 4
        let countCount = stream.Read(countBytes, 0, 4)
        if countCount <> 4 then
            fail "Communication with CLR: could not get the amount of bytes of the next message. Instead read %d bytes" countCount
        BitConverter.ToInt32(countBytes, 0)

    let readBuffer () =
        let chunkSize = 8192
        let count = readCount()
//        assert(count <> 0) // TODO: do we need empty messages? #do
        if count < 0 then None
        else
            writeConfirmation()
            let buffer : byte[] = Array.zeroCreate count
            let mutable bytesRead = 0
            while bytesRead < count do
                let length = min chunkSize (count - bytesRead)
                let chunk : byte[] = Array.zeroCreate length
                let offset = bytesRead
                bytesRead <- bytesRead + server.Read(chunk, 0, length)
                Array.Copy(chunk, 0, buffer, offset, length)
            if bytesRead <> count then
                fail "Communication with CLR: expected %d bytes, but read %d bytes" count bytesRead
            else
                writeConfirmation()
                Some buffer

    let writeBuffer (buffer : byte[]) =
        if buffer.LongLength > int64(Int32.MaxValue) then
            fail "Communication with CLR: too large message (length = %s)!" (buffer.LongLength.ToString())
        let countBuffer = BitConverter.GetBytes(buffer.Length)
        assert(countBuffer.Length = 4)
        stream.Write(countBuffer, 0, 4)
        readConfirmation()
        stream.Write(buffer, 0, buffer.Length)
        readConfirmation()

    let readString () =
        match readBuffer() with
        | None -> unexpectedlyTerminated()
        | Some buffer -> Encoding.ASCII.GetString(buffer)

    // NOTE: all strings, sent to concolic should end with null terminator
    let writeString (str : string) =
        // NOTE: adding null terminator
        let buffer = Encoding.ASCII.GetBytes(str + Char.MinValue.ToString())
        writeBuffer buffer

    let waitClient () =
        Logger.trace "Waiting for client connection..."
        server.WaitForConnection()
        Logger.trace "Client connected!"

    let handshake () =
        let message = "Hi!"
        writeString message
        let expectedMessage = "Hi!"
        let s = readString ()
        if s <> expectedMessage then
            fail "Communication with CLR: handshake failed: got %s instead of %s" s expectedMessage

    override x.Finalize() =
        server.Close()

    member private x.Deserialize<'a> (bytes : byte array, startIndex : int) =
        let result = Reflection.createObject typeof<'a> :?> 'a
        let size = Marshal.SizeOf(typeof<'a>)
        let unmanagedPtr = Marshal.AllocHGlobal(size)
        Marshal.Copy(bytes, startIndex, unmanagedPtr, size)
        Marshal.PtrToStructure(unmanagedPtr, result)
        Marshal.FreeHGlobal(unmanagedPtr)
        result

    member private x.Deserialize<'a> (bytes : byte array) = x.Deserialize<'a>(bytes, 0)

    member private x.Serialize<'a> (structure : 'a, bytes : byte array, startIndex : int) =
        let size = Marshal.SizeOf(typeof<'a>)
        let unmanagedPtr = Marshal.AllocHGlobal(size)
        Marshal.StructureToPtr(structure, unmanagedPtr, false)
        Marshal.Copy(unmanagedPtr, bytes, startIndex, size)
        Marshal.FreeHGlobal(unmanagedPtr)

    member private x.Serialize<'a> (structure : 'a) =
        let size = Marshal.SizeOf(typeof<'a>)
        let result = Array.zeroCreate size
        x.Serialize<'a>(structure, result, 0)
        result

    member private x.SerializeCommand command =
        let byte =
            match command with
            | ReadString -> readStringByte
            | ReadMethodBody -> readMethodBodyByte
            | ParseTypesInfoFromMethod -> parseTypesInfoByte
            | ParseTypeRef -> parseTypeRefByte
            | ParseTypeSpec -> parseTypeSpecByte
            | ReadHeapBytes -> readHeapBytesByte
            | ReadExecResponse -> readExecResponseByte
            | Unmarshall -> unmarshallByte
            | UnmarshallArray -> unmarshallArrayByte
            | ReadWholeObject -> readWholeObjectByte
            | ReadArray -> readArrayByte
            | ParseFieldRefTypeToken -> parseFieldRefTypeTokenByte
            | ParseFieldDefTypeToken -> parseFieldDefTypeTokenByte
            | ParseArgTypeToken -> parseArgTypeTokenByte
            | ParseLocalTypeToken -> parseLocalTypeTokenByte
            | ParseReturnTypeToken -> parseReturnTypeTokenByte
            | ParseDeclaringTypeToken -> parseDeclaringTypeTokenByte
        Array.singleton byte

    member x.Connect() =
        try
            waitClient()
            handshake()
            true
        with
        | :? IOException as e -> reportError e

    member private x.ReadStructure<'a>() =
        match readBuffer() with
        | Some bytes -> x.Deserialize<'a> bytes
        | None -> unexpectedlyTerminated()

    member x.ReadProbes() = x.ReadStructure<probes>()

    member x.SendEntryPoint (m : Reflection.MethodBase) =
        let moduleName = m.Module.FullyQualifiedName
        let moduleNameBytes = Encoding.Unicode.GetBytes moduleName
        let moduleSize = BitConverter.GetBytes moduleName.Length
        let methodDef = BitConverter.GetBytes m.MetadataToken
        Array.concat [moduleSize; methodDef; moduleNameBytes] |> writeBuffer

    member x.SendCoverageInformation (cov : coverageLocation list) =
        Logger.trace "send coverage %O" cov
        let sizeOfLocation = Marshal.SizeOf(typeof<coverageLocation>)
        let entriesCount = List.length cov
        let bytes : byte[] = Array.zeroCreate (sizeof<int32> + entriesCount * sizeOfLocation)
        x.Serialize<int>(entriesCount, bytes, 0)
        List.iteri (fun i loc ->
            let idx = sizeof<int32> + (entriesCount - i - 1) * sizeOfLocation
            x.Serialize<int>(loc.moduleToken, bytes, idx)
            x.Serialize<int>(loc.methodToken, bytes, idx + sizeof<int32>)
            x.Serialize<int>(loc.offset, bytes, idx + 2 * sizeof<int32>)
            x.Serialize<int>(loc.threadToken, bytes, idx + 3 * sizeof<int32>)
            x.Serialize<byte>(loc.stackPush, bytes, idx + 4 * sizeof<int32>)) cov
        writeBuffer bytes

    member x.SendCommand (command : commandForConcolic) =
        let bytes = x.SerializeCommand command
        writeBuffer bytes

    member x.SendStringAndReadItsIndex (str : string) : uint32 =
        x.SendCommand ReadString
        writeString str
        match readBuffer() with
        | Some bytes -> BitConverter.ToUInt32(bytes, 0)
        | None -> unexpectedlyTerminated()

    member x.SendMethodTokenAndParseTypes (methodToken : int) : uint32[] =
        x.SendCommand ParseTypesInfoFromMethod
        x.Serialize<int> methodToken |> writeBuffer
        match readBuffer() with
        | Some bytes ->
            let typesSize = Array.length bytes / sizeof<uint32>
            Array.init typesSize (fun i -> BitConverter.ToUInt32(bytes, i * sizeof<uint32>))
        | None -> Array.empty

    member private x.ReadTypeToken() : uint32 =
        match readBuffer() with
        | Some bytes ->
            assert(Array.length bytes = sizeof<uint32>)
            BitConverter.ToUInt32(bytes)
        | None -> internalfail "expected type token, but got nothing"

    member private x.SendStringAndParseTypeToken (string : string) : uint32 =
        Encoding.Unicode.GetBytes string |> writeBuffer
        x.ReadTypeToken()

    member x.SendStringAndParseTypeRef (string : string) : uint32 =
        x.SendCommand ParseTypeRef
        x.SendStringAndParseTypeToken string

    member x.SendStringAndParseTypeSpec (string : string) : uint32 =
        x.SendCommand ParseTypeSpec
        x.SendStringAndParseTypeToken string

    member x.ReadHeapBytes (address : UIntPtr) offset size refOffsets : byte[] =
        x.SendCommand ReadHeapBytes
        let refOffsetBytes = Array.collect x.Serialize<int> refOffsets
        let offsetsLength = Array.length refOffsets |> x.Serialize<int>
        Array.concat [
            x.Serialize<UIntPtr> address; x.Serialize<int> offset
            x.Serialize<int> size; offsetsLength; refOffsetBytes
        ] |> writeBuffer
        match readBuffer() with
        | Some bytes -> bytes
        | None -> internalfail "Reading bytes from concolic: got nothing"

    member private x.SendParametersAndReadObject (address : UIntPtr) refOffsets : byte[] =
        let refOffsetBytes = Array.collect x.Serialize<int> refOffsets
        let offsetsLength = Array.length refOffsets |> x.Serialize<int>
        Array.concat [x.Serialize<UIntPtr> address; offsetsLength; refOffsetBytes] |> writeBuffer
        match readBuffer() with
        | Some bytes -> bytes
        | None -> internalfail "Reading bytes from concolic: got nothing"

    member private x.SendParametersAndReadArray (address : UIntPtr) elemSize refOffsets : byte[] =
        let refOffsetBytes = Array.collect x.Serialize<int> refOffsets
        let offsetsLength = Array.length refOffsets |> x.Serialize<int>
        Array.concat [x.Serialize<UIntPtr> address; x.Serialize<int32> elemSize; offsetsLength; refOffsetBytes] |> writeBuffer
        match readBuffer() with
        | Some bytes -> bytes
        | None -> internalfail "Reading bytes from concolic: got nothing"

    // NOTE: 'Unmarshall' and 'UnmarshallArray' cases are divided to
    //       justify resolving references inside elements in Conoclic

    // NOTE: function 'Unmarshall' is used only for non-array objects
    member x.Unmarshall (address : UIntPtr) refOffsets : byte[] =
        x.SendCommand Unmarshall
        x.SendParametersAndReadObject address refOffsets

    // NOTE: function 'UnmarshallArray' is used only for arrays
    member x.UnmarshallArray (address : UIntPtr) elemSize refOffsets : byte[] =
        x.SendCommand UnmarshallArray
        x.SendParametersAndReadArray address elemSize refOffsets

    // NOTE: is used for all objects except arrays
    member x.ReadWholeObject (address : UIntPtr) refOffsets : byte[] =
        x.SendCommand ReadWholeObject
        x.SendParametersAndReadObject address refOffsets

    // NOTE: function 'ReadArray' is used only for arrays
    member x.ReadArray (address : UIntPtr) elemSize refOffsets : byte[] =
        x.SendCommand ReadArray
        x.SendParametersAndReadArray address elemSize refOffsets

    member x.ParseFieldRefTypeToken (fieldRef : int) : uint32 =
        x.SendCommand ParseFieldRefTypeToken
        x.Serialize<int> fieldRef |> writeBuffer
        x.ReadTypeToken()

    member x.ParseFieldDefTypeToken (fieldDef : int) : uint32 =
        x.SendCommand ParseFieldDefTypeToken
        x.Serialize<int> fieldDef |> writeBuffer
        x.ReadTypeToken()

    member x.ParseArgTypeToken (methodToken : int) (argIndex : int) : uint32 =
        x.SendCommand ParseArgTypeToken
        Array.concat [x.Serialize<int> methodToken; x.Serialize<int> argIndex] |> writeBuffer
        x.ReadTypeToken()

    member x.ParseLocalTypeToken (localIndex : int) : uint32 =
        x.SendCommand ParseLocalTypeToken
        x.Serialize<int> localIndex |> writeBuffer
        x.ReadTypeToken()

    member x.ParseReturnTypeToken () : uint32 =
        x.SendCommand ParseReturnTypeToken
        x.ReadTypeToken()

    member x.ParseDeclaringTypeToken (methodToken : int) : uint32 =
        x.SendCommand ParseDeclaringTypeToken
        x.Serialize<int> methodToken |> writeBuffer
        x.ReadTypeToken()

    member x.ReadMethodBody() =
        match readBuffer() with
        | Some bytes ->
            let propertiesBytes, rest = Array.splitAt (Marshal.SizeOf typeof<rawMethodProperties>) bytes
            let properties = x.Deserialize<rawMethodProperties> propertiesBytes
            let sizeOfSignatureTokens = Marshal.SizeOf typeof<signatureTokens>
            if int properties.signatureTokensLength <> sizeOfSignatureTokens then
                fail "Size of received signature tokens buffer mismatch the expected! Probably you've altered the client-side signatures, but forgot to alter the server-side structure (or vice-versa)"
            let signatureTokenBytes, rest = Array.splitAt sizeOfSignatureTokens rest
            let assemblyNameBytes, rest = Array.splitAt (int properties.assemblyNameLength) rest
            let moduleNameBytes, rest = Array.splitAt (int properties.moduleNameLength) rest
            let signatureTokens = x.Deserialize<signatureTokens> signatureTokenBytes
            let assemblyName = Encoding.Unicode.GetString(assemblyNameBytes)
            let moduleName = Encoding.Unicode.GetString(moduleNameBytes)
            let ilBytes, ehBytes  = Array.splitAt (int properties.ilCodeSize) rest
            let ehSize = Marshal.SizeOf typeof<rawExceptionHandler>
            let ehCount = Array.length ehBytes / ehSize
            let ehs = Array.init ehCount (fun i -> x.Deserialize<rawExceptionHandler>(ehBytes, i * ehSize))
            {properties = properties; tokens = signatureTokens; assembly = assemblyName; moduleName = moduleName; il = ilBytes; ehs = ehs}
        | None -> unexpectedlyTerminated()

    member private x.corElementTypeToType (elemType : CorElementType) =
        match elemType with
        | CorElementType.ELEMENT_TYPE_BOOLEAN -> Some(typeof<bool>)
        | CorElementType.ELEMENT_TYPE_CHAR    -> Some(typeof<char>)
        | CorElementType.ELEMENT_TYPE_I1      -> Some(typeof<int8>)
        | CorElementType.ELEMENT_TYPE_U1      -> Some(typeof<uint8>)
        | CorElementType.ELEMENT_TYPE_I2      -> Some(typeof<int16>)
        | CorElementType.ELEMENT_TYPE_U2      -> Some(typeof<uint16>)
        | CorElementType.ELEMENT_TYPE_I4      -> Some(typeof<int32>)
        | CorElementType.ELEMENT_TYPE_U4      -> Some(typeof<uint32>)
        | CorElementType.ELEMENT_TYPE_I8      -> Some(typeof<int64>)
        | CorElementType.ELEMENT_TYPE_U8      -> Some(typeof<uint64>)
        | CorElementType.ELEMENT_TYPE_R4      -> Some(typeof<float32>)
        | CorElementType.ELEMENT_TYPE_R8      -> Some(typeof<double>)
        | CorElementType.ELEMENT_TYPE_I       -> Some(typeof<IntPtr>)
        | CorElementType.ELEMENT_TYPE_U       -> Some(typeof<UIntPtr>)
        | _ -> None

    member private x.ParseType(bytes : byte array, offset : int byref) =
        let mutable currentOffset = offset
        let rec readType() =
            let isValid = bytes[currentOffset] = 1uy
            currentOffset <- currentOffset + sizeof<byte>
            if isValid then
                let isArray = BitConverter.ToBoolean(bytes, currentOffset)
                currentOffset <- currentOffset + sizeof<bool>
                if isArray then
                    let corElementType = Microsoft.FSharp.Core.LanguagePrimitives.EnumOfValue<byte, CorElementType>(bytes.[currentOffset])
                    currentOffset <- currentOffset + sizeof<byte>
                    let rank = BitConverter.ToInt32(bytes, currentOffset)
                    currentOffset <- currentOffset + sizeof<int32>
                    let t = Option.defaultWith readType (x.corElementTypeToType corElementType)
                    if rank = 1 then t.MakeArrayType() else t.MakeArrayType(rank)
                else
                    let token = BitConverter.ToInt32(bytes, currentOffset)
                    currentOffset <- currentOffset + sizeof<int>
                    let assemblySize = BitConverter.ToInt32(bytes, currentOffset)
                    currentOffset <- currentOffset + sizeof<int>
                    // NOTE: truncating null terminator
                    let assemblyBytes = bytes.[currentOffset .. currentOffset + assemblySize - 3]
                    currentOffset <- currentOffset + assemblySize
                    let assemblyName = Encoding.Unicode.GetString(assemblyBytes)
                    let assembly = Reflection.loadAssembly assemblyName
                    let moduleSize = BitConverter.ToInt32(bytes, currentOffset)
                    currentOffset <- currentOffset + sizeof<int>
                    let moduleBytes = bytes.[currentOffset .. currentOffset + moduleSize - 1]
                    currentOffset <- currentOffset + moduleSize
                    let moduleName = Encoding.Unicode.GetString(moduleBytes) |> Path.GetFileName
                    let typeModule = Reflection.resolveModuleFromAssembly assembly moduleName
                    let typeArgsCount = BitConverter.ToInt32(bytes, currentOffset)
                    currentOffset <- currentOffset + sizeof<int>
                    let typeArgs = Array.init typeArgsCount (fun _ -> readType())
                    let resultType = Reflection.resolveTypeFromModule typeModule token
                    if Array.isEmpty typeArgs then resultType else resultType.MakeGenericType(typeArgs)
            else typeof<Void>
        let parsedType = readType()
        offset <- currentOffset
        parsedType

    member private x.ParseRef(bytes : byte array, offset : int byref) =
        let mutable currentOffset = offset
        let parseFrameAndIdx() =
            let frame = bytes[currentOffset]
            currentOffset <- currentOffset + 1
            let idx = bytes[currentOffset]
            currentOffset <- currentOffset + 1
            frame, idx
        let parseStaticFieldId() =
            let id = BitConverter.ToInt16(bytes, currentOffset)
            currentOffset <- currentOffset + 2
            id
        let baseAddr = Reflection.BitConverterToUIntPtr bytes currentOffset
        currentOffset <- currentOffset + UIntPtr.Size
        let shift = Reflection.BitConverterToUIntPtr bytes currentOffset
        currentOffset <- currentOffset + UIntPtr.Size
        let locationType = bytes[currentOffset]
        currentOffset <- currentOffset + 1
        let key =
            match locationType with
            | 1uy -> currentOffset <- currentOffset + 2; ReferenceType
            | 2uy -> LocalVariable(parseFrameAndIdx())
            | 3uy -> Parameter(parseFrameAndIdx())
            | 4uy -> Statics(parseStaticFieldId())
            | 5uy -> TemporaryAllocatedStruct(parseFrameAndIdx())
            | _ -> internalfailf "ReadExecuteCommand: unexpected object location type: %O" locationType
        offset <- currentOffset
        baseAddr, shift, key

    member x.ReadExecuteCommand() =
        match readBuffer() with
        | Some bytes ->
            let staticSize = Marshal.SizeOf typeof<execCommandStatic>
            let staticBytes, dynamicBytes = Array.splitAt staticSize bytes
            let staticPart = x.Deserialize<execCommandStatic> staticBytes
            let exceptionIsConcrete = staticPart.exceptionIsConcrete = 1uy
            let exceptionRegister =
                match staticPart.exceptionKind with
                | 1uy -> UnhandledConcolic(staticPart.exceptionRegister, exceptionIsConcrete)
                | 2uy -> CaughtConcolic(staticPart.exceptionRegister, exceptionIsConcrete)
                | 3uy -> NoExceptionConcolic
                | ek -> internalfailf "ReadExecuteCommand: unexpected exception kind %O" ek
            let terminatedByException = staticPart.isTerminatedByException = 1uy
            let callStackEntrySize = sizeof<int32> * 2
            let callStackOffset = (int staticPart.newCallStackFramesCount) * callStackEntrySize
            let newCallStackFrames = Array.init (int staticPart.newCallStackFramesCount) (fun i ->
                let resolvedToken = BitConverter.ToInt32(dynamicBytes, i * callStackEntrySize)
                let unresolvedToken = BitConverter.ToInt32(dynamicBytes, i * callStackEntrySize + sizeof<int32>)
                resolvedToken, unresolvedToken)
            let mutable offset = callStackOffset
            let thisAddresses = Array.init (int staticPart.newCallStackFramesCount) (fun _ ->
                x.ParseRef(dynamicBytes, &offset))
            let ipStack = List.init (int staticPart.ipStackCount) (fun _ ->
                let res = BitConverter.ToInt32(dynamicBytes, offset) in offset <- offset + sizeof<int>; res)
            let evaluationStackPushes = Array.init (int staticPart.evaluationStackPushesCount) (fun _ ->
                let evalStackArgTypeNum = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let evalStackArgType = LanguagePrimitives.EnumOfValue evalStackArgTypeNum
                match evalStackArgType with
                | evalStackArgType.OpRef ->
                    let baseAddr, shift, key = x.ParseRef(dynamicBytes, &offset)
                    PointerOp(baseAddr, shift, key)
                | evalStackArgType.OpSymbolic
                | evalStackArgType.OpI4
                | evalStackArgType.OpI8
                | evalStackArgType.OpR4
                | evalStackArgType.OpR8 ->
                    let content = BitConverter.ToInt64(dynamicBytes, offset)
                    offset <- offset + sizeof<int64>
                    NumericOp(evalStackArgType, content)
                | evalStackArgType.OpEmpty -> EmptyOp
                | evalStackArgType.OpStruct ->
                    internalfail "ReadExecuteCommand: struct case is not implemented"
                | _ -> internalfailf "unexpected evaluation stack argument type %O" evalStackArgType)
            let newAddresses = Array.init (int staticPart.newAddressesCount) (fun _ ->
                let res = Reflection.BitConverterToUIntPtr dynamicBytes offset in offset <- offset + UIntPtr.Size; res)
            let deletedAddresses = Array.init (int staticPart.deletedAddressesCount) (fun _ ->
                let res = Reflection.BitConverterToUIntPtr dynamicBytes offset in offset <- offset + UIntPtr.Size; res)
            let delegateCoupling = Array.init (int staticPart.delegatesCount) (fun _ ->
                let actionPtr = Reflection.BitConverterToUIntPtr dynamicBytes offset in offset <- offset + UIntPtr.Size
                let functionPtr = Reflection.BitConverterToUIntPtr dynamicBytes offset in offset <- offset + UIntPtr.Size
                let closureRef = Reflection.BitConverterToUIntPtr dynamicBytes offset in offset <- offset + UIntPtr.Size
                actionPtr, functionPtr, closureRef)
            let newAddressesTypesLengths = Array.init (int staticPart.newAddressesCount) (fun _ ->
                let res = BitConverter.ToUInt64(dynamicBytes, offset) in offset <- offset + sizeof<uint64>; res)
            let newAddressesTypes = Array.init (int staticPart.newAddressesCount) (fun i ->
                let size = int newAddressesTypesLengths.[i]
                x.ParseType(dynamicBytes, &offset))
            let newCoverageNodesCount = BitConverter.ToInt32(dynamicBytes, offset)
            offset <- offset + sizeof<int32>
            let mutable newCoveragePath = []
            for i in 1 .. newCoverageNodesCount do
                let moduleToken = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let methodToken = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let ilOffset = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let threadToken = BitConverter.ToInt32(dynamicBytes, offset)
                offset <- offset + sizeof<int32>
                let stackPush = dynamicBytes[offset]
                offset <- offset + sizeof<byte>
                let node : coverageLocation = {moduleToken = moduleToken; methodToken = methodToken; offset = ilOffset; threadToken = threadToken; stackPush = stackPush}
                newCoveragePath <- node::newCoveragePath
            { isBranch = staticPart.isBranch
              callStackFramesPops = staticPart.callStackFramesPops
              evaluationStackPops = staticPart.evaluationStackPops
              exceptionRegister = exceptionRegister
              isTerminatedByException = terminatedByException
              newCallStackFrames = newCallStackFrames
              thisAddresses = thisAddresses
              ipStack = ipStack
              evaluationStackPushes = evaluationStackPushes
              newAddresses = newAddresses
              deletedAddresses = deletedAddresses
              delegateCoupling = delegateCoupling
              newAddressesTypes = newAddressesTypes
              newCoveragePath = newCoveragePath }
        | None -> unexpectedlyTerminated()

    member private x.SizeOfConcrete (typ : Core.symbolicType) =
        if Types.IsValueType typ then sizeof<int> + sizeof<int64>
        else sizeof<int> + 2 * sizeof<int64>

    member private x.IntegerBytesToLong (obj : obj) =
        let extended =
            match obj with
            | :? byte as v -> int64 v |> BitConverter.GetBytes
            | :? sbyte as v -> int64 v |> BitConverter.GetBytes
            | :? int16 as v -> int64 v |> BitConverter.GetBytes
            | :? uint16 as v -> int64 v |> BitConverter.GetBytes
            | :? char as v -> int64 v |> BitConverter.GetBytes
            | :? int32 as v -> int64 v |> BitConverter.GetBytes
            | :? uint32 as v -> uint64 v |> BitConverter.GetBytes
            | :? int64 as v -> BitConverter.GetBytes v
            | :? uint64 as v -> BitConverter.GetBytes v
            | _ -> internalfailf "IntegerBytesToLong: unexpected object %O" obj
        BitConverter.ToInt64 extended

    member private x.SerializeConcrete (obj : obj, typ : Core.symbolicType) =
        let bytes = x.SizeOfConcrete typ |> Array.zeroCreate
        let mutable index = 0
        if Types.IsValueType typ then
            let opType, (content : int64) =
                if Types.IsInteger typ then
                    let typ = if Types.SizeOf typ = sizeof<int64> then evalStackArgType.OpI8 else evalStackArgType.OpI4
                    typ, x.IntegerBytesToLong obj
                elif Types.IsReal typ then
                    if Types.SizeOf typ = sizeof<double> then
                        evalStackArgType.OpR8, BitConverter.DoubleToInt64Bits (obj :?> double)
                    else evalStackArgType.OpR4, BitConverter.DoubleToInt64Bits (obj :?> float |> double)
                elif Types.IsBool typ then
                    evalStackArgType.OpI4, if obj :?> bool then 1L else 0L
                else
                    // TODO: support structs
                    __notImplemented__()
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue opType) in assert success
            index <- index + sizeof<int>
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int64>), content) in assert success
            index <- index + sizeof<int64>
        elif isNull obj then
            // NOTE: null refs handling
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue evalStackArgType.OpRef) in assert success
            index <- index + sizeof<int>
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), 0UL) in assert success
            index <- index + sizeof<uint64>
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), 0UL) in assert success
            index <- index + sizeof<uint64>
        else
            // NOTE: nonnull refs handling
            let address, offset = obj :?> UIntPtr * int32
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<int>), LanguagePrimitives.EnumToValue evalStackArgType.OpRef) in assert success
            index <- index + sizeof<int>
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), uint64 address) in assert success
            index <- index + sizeof<int64>
            let success = BitConverter.TryWriteBytes(Span(bytes, index, sizeof<uint64>), uint64 offset) in assert success
            index <- index + sizeof<int64>
        bytes

    member private x.SerializeOperands (ops : (obj * Core.symbolicType) list) =
        let mutable index = 0
        let bytesCount = ops |> List.sumBy (snd >> x.SizeOfConcrete)
        let bytes = Array.zeroCreate bytesCount
        ops |> List.iter (fun concrete ->
            let op = x.SerializeConcrete concrete
            let size = op.Length
            Array.blit op 0 bytes index size
            index <- index + size)
        bytes

    member x.SerializeStackPush (lastStackPush : bool option) =
        match lastStackPush with
        | Some isConcrete when isConcrete -> 2uy
        | Some _ -> 1uy
        | None -> 0uy

    member x.SendExecResponse (ops : (obj * Core.symbolicType) list option) (result : (obj * Core.symbolicType) option) (lastPush : byte) (framesCount : int) =
        x.SendCommand ReadExecResponse
        let len, opsBytes =
            match ops with
            | Some ops -> ops.Length, x.SerializeOperands ops
            | None -> -1, Array.empty
        let hasInternalCallResult, resultBytes =
            match result with
            | Some r -> 1uy, x.SerializeConcrete r
            | None -> 0uy, Array.empty
        let staticPart = { framesCount = uint framesCount; lastPush = lastPush; opsLength = len; hasResult = hasInternalCallResult }
        let staticPartBytes = x.Serialize<execResponseStaticPart> staticPart
        let message = Array.concat [staticPartBytes; opsBytes; resultBytes]
        Logger.trace "Sending exec response! Total %d bytes" message.Length
        writeBuffer message

    member x.SendMethodBody (mb : instrumentedMethodBody) =
        x.SendCommand ReadMethodBody
        let propBytes = x.Serialize mb.properties
        let ehSize = Marshal.SizeOf typeof<rawExceptionHandler>
        let ehBytes : byte[] = Array.zeroCreate (ehSize * mb.ehs.Length)
        Array.iteri (fun i eh -> x.Serialize<rawExceptionHandler>(eh, ehBytes, i * ehSize)) mb.ehs
        let message = Array.concat [propBytes; mb.il; ehBytes]
        Logger.trace "Sending method body! Total %d bytes" message.Length
        writeBuffer message

    member x.ReadCommand() =
        match readBuffer() with
        | Some bytes ->
            if bytes.Length <> 1 then fail "Invalid command number!"
            match bytes.[0] with
            | b when b = instrumentCommandByte ->
                x.ReadMethodBody() |> Instrument
            | b when b = executeCommandByte ->
                x.ReadExecuteCommand() |> ExecuteInstruction
            | b -> fail "Unexpected command %d from client machine!" b
        | None -> Terminate

    interface IDisposable with
        member x.Dispose() =
            server.Dispose()
