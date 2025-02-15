namespace VSharp.Core

open VSharp
open VSharp.CSharpUtils
open VSharp.TypeUtils
open EnumUtils
open System
open System.Collections.Generic

type IMethod =
    inherit IComparable
    abstract Name : string
    abstract FullName : string
    abstract ReturnType : Type
    abstract DeclaringType : Type
    abstract ReflectedType : Type
    abstract Parameters : Reflection.ParameterInfo[]
    abstract LocalVariables : IList<Reflection.LocalVariableInfo>
    abstract HasThis : bool
    abstract IsConstructor : bool
    abstract GenericArguments : Type[]
    abstract SubstituteTypeVariables : (Type -> Type) -> IMethod
    abstract MethodBase : System.Reflection.MethodBase

[<CustomEquality;CustomComparison>]
type stackKey =
    | ThisKey of IMethod
    | ParameterKey of Reflection.ParameterInfo
    | LocalVariableKey of Reflection.LocalVariableInfo * IMethod
    | TemporaryLocalVariableKey of Type * int
    override x.ToString() =
        match x with
        | ThisKey _ -> "this"
        | ParameterKey pi -> if String.IsNullOrEmpty pi.Name then "#" + toString pi.Position else pi.Name
        | LocalVariableKey (lvi,_) -> "__loc__" + lvi.LocalIndex.ToString()
        | TemporaryLocalVariableKey (typ, index) -> sprintf "__tmp__%s%d" (Reflection.getFullTypeName typ) index
    override x.GetHashCode() =
        let fullname =
            match x with
            | ThisKey m -> sprintf "%s##this" m.FullName
            | ParameterKey pi -> sprintf "%O##%O##%d" pi.Member pi pi.Position
            | LocalVariableKey (lvi, m) -> sprintf "%O##%s" m.FullName (lvi.ToString())
            | TemporaryLocalVariableKey (typ, index) -> sprintf "temporary##%s%d" (Reflection.getFullTypeName typ) index
        fullname.GetDeterministicHashCode()
    interface IComparable with
        override x.CompareTo(other: obj) =
            match other with
            | :? stackKey as other ->
                match x, other with
                | ThisKey _, ThisKey _
                | ParameterKey _, ParameterKey _
                | LocalVariableKey _, LocalVariableKey _
                | TemporaryLocalVariableKey _, TemporaryLocalVariableKey _ -> x.GetHashCode().CompareTo(other.GetHashCode())
                | ThisKey _, _ -> -1
                | _, ThisKey _ -> 1
                | LocalVariableKey _, _ -> -1
                | _, LocalVariableKey _ -> 1
                | TemporaryLocalVariableKey _, _ -> -1
                | _, TemporaryLocalVariableKey _ -> 1
            | _ -> -1
    override x.Equals(other) = (x :> IComparable).CompareTo(other) = 0
    member x.TypeOfLocation =
        match x with
        | ThisKey m -> m.DeclaringType
        | ParameterKey p -> p.ParameterType
        | LocalVariableKey(l, _) -> l.LocalType
        | TemporaryLocalVariableKey (typ, _) -> typ
    member x.Map typeSubst =
        match x with
        | ThisKey m -> ThisKey (m.SubstituteTypeVariables typeSubst)
        | ParameterKey p -> ParameterKey (Reflection.concretizeParameter p typeSubst)
        | LocalVariableKey(l, m) ->
            let m' = m.SubstituteTypeVariables typeSubst
            let l' = m.LocalVariables.[l.LocalIndex]
            LocalVariableKey(l', m')
        | TemporaryLocalVariableKey (typ, index) ->
            // TODO: index may become inconsistent here
            TemporaryLocalVariableKey((Reflection.concretizeType typeSubst typ), index)

type concreteHeapAddress = vectorTime
type arrayType = Type * int * bool // Element type * dimension * is vector

[<StructuralEquality;NoComparison>]
type operation =
    | Operator of OperationType
    | Application of StandardFunction
    | Cast of Type * Type
    | Combine
    member x.priority =
        match x with
        | Operator op -> Operations.operationPriority op
        | Application _ -> Operations.maxPriority
        | Cast _ -> Operations.maxPriority - 1
        | Combine -> Operations.maxPriority

// TODO: symbolic type -> primitive type
// TODO: get rid of Nop!
[<StructuralEquality;NoComparison>]
type termNode =
    | Nop
    | Concrete of obj * Type
    | Constant of string transparent * ISymbolicConstantSource * Type
    | Expression of operation * term list * Type
    | Struct of pdict<fieldId, term> * Type
    | HeapRef of heapAddress * Type
    | Ref of address
    // NOTE: use ptr only in case of reinterpretation: changed sight type or address arithmetic, otherwise use ref instead
    | Ptr of pointerBase * Type * term // base address * sight type * offset (in bytes)
    | Slice of term * term * term * term // what term to slice * start byte * end byte * position inside combine
    | Union of (term * term) list

    override x.ToString() =
        let getTerm (term : term) = term.term

        let checkExpression priority parentPriority =
            if priority < parentPriority then sprintf "(%s)" else id

        let formatIfNotEmpty format value =
            match value with
            | _ when String.IsNullOrEmpty value -> value
            | _ -> format value

        let formatWithIndent indent = sprintf "\n%s%s" indent

        let extendIndent = (+) "\t"

        let rec toStr parentPriority indent term k =
            match term with
            | Nop -> k "<VOID>"
            | Constant(name, _, _) -> k name.v
            | Concrete(_, (ClassType(t, _) as typ)) when isSubtypeOrEqual t typedefof<Delegate> ->
                sprintf "<Lambda Expression %O>" typ |> k
            | Concrete(obj, AddressType) when (obj :?> int32 list) = [0] -> k "null"
            | Concrete(c, Char) when c :?> char = '\000' -> k "'\\000'"
            | Concrete(c, Char) -> sprintf "'%O'" c |> k
            | Concrete(:? concreteHeapAddress as addr, AddressType) -> VectorTime.print addr |> k
            | Concrete(value, _) -> value.ToString() |> k
            | Expression(operation, operands, _) ->
                match operation with
                | Operator operator when Operations.operationArity operator = 1 ->
                    assert (List.length operands = 1)
                    let operand = List.head operands
                    let opStr = Operations.operationToString operator |> checkExpression operation.priority parentPriority
                    toStr operation.priority indent operand.term (fun printedOperand ->
                    sprintf (Printf.StringFormat<string->string>(opStr)) printedOperand |> k)
                | Operator operator ->
                    assert (List.length operands >= 2)
                    Cps.List.mapk (fun x k -> toStr operation.priority indent (getTerm x) k) operands (fun printedOperands ->
                    let sortedOperands = if Operations.isCommutative operator then List.sort printedOperands else printedOperands
                    sortedOperands
                        |> String.concat (Operations.operationToString operator)
                        |> checkExpression operation.priority parentPriority
                        |> k)
                | Cast(_, dest) ->
                    assert (List.length operands = 1)
                    toStr operation.priority indent (List.head operands).term (fun term ->
                    sprintf "(%O %s)" dest term
                        |> checkExpression operation.priority parentPriority
                        |> k)
                | Application f ->
                    Cps.List.mapk (getTerm >> toStr -1 indent) operands (fun results ->
                    results |> join ", " |> sprintf "%O(%s)" f |> k)
                | Combine ->
                    Cps.List.mapk (getTerm >> toStr -1 indent) operands (fun results ->
                    results |> join ", " |> sprintf "Combine(%s)" |> k)
            | Struct(fields, t) ->
                fieldsToString indent fields |> sprintf "%O STRUCT [%s]" t |> k
            | Union(guardedTerms) ->
                let guardedToString (guard, term) k =
                    toStringWithParentIndent indent guard (fun guardString ->
                    toStringWithParentIndent indent term (fun termString ->
                    sprintf "| %s ~> %s" guardString termString |> k))
                Cps.Seq.mapk guardedToString guardedTerms (fun guards ->
                let printed = guards |> Seq.sort |> join ("\n" + indent)
                formatIfNotEmpty (formatWithIndent indent) printed |> sprintf "UNION[%s]" |> k)
            | HeapRef({term = Concrete(obj, AddressType)}, _) when (obj :?> int32 list) = [0] -> k "NullRef"
            | HeapRef(address, baseType) -> sprintf "(HeapRef %O to %O)" address baseType |> k
            | Ref address -> sprintf "(%sRef %O)" (address.Zone()) address |> k
            | Ptr(address, typ, shift) ->
                let offset = ", offset = " + shift.ToString()
                sprintf "(%sPtr %O as %O%s)" (address.Zone()) address typ offset |> k
            | Slice(term, offset, size, _) -> sprintf "Slice(%O, %O, %O)" term offset size |> k

        and fieldsToString indent fields =
            let stringResult = PersistentDict.toString "| %O ~> %O" ("\n" + indent) toString toString toString fields
            formatIfNotEmpty (formatWithIndent indent) stringResult

        and toStringWithIndent indent term k = toStr -1 indent term.term k

        and toStringWithParentIndent parentIndent term k = toStringWithIndent (extendIndent parentIndent) term k

        toStr -1 "\t" x id

and heapAddress = term // only Concrete(:? concreteHeapAddress) or Constant of type AddressType!

and pointerBase =
    | StackLocation of stackKey
    | HeapLocation of heapAddress * Type // Null or virtual address * sight type of address
    | StaticLocation of Type
    member x.Zone() =
        match x with
        | StackLocation _ -> "Stack"
        | HeapLocation _ -> "Heap"
        | StaticLocation _ -> "Statics"

and address =
    | PrimitiveStackLocation of stackKey
    | StructField of address * fieldId
    | StackBufferIndex of stackKey * term
    | BoxedLocation of concreteHeapAddress * Type // TODO: delete type from boxed location?
    | ClassField of heapAddress * fieldId
    | ArrayIndex of heapAddress * term list * arrayType
    | ArrayLowerBound of heapAddress * term * arrayType
    | ArrayLength of heapAddress * term * arrayType
    | StaticField of Type * fieldId
    override x.ToString() =
        match x with
        | PrimitiveStackLocation key -> toString key
        | ClassField(addr, field) -> sprintf "%O.%O" addr field
        | ArrayIndex(addr, idcs, _) -> sprintf "%O[%s]" addr (List.map toString idcs |> join ", ")
        | StaticField(typ, field) -> sprintf "%O.%O" typ field
        | StructField(addr, field) -> sprintf "%O.%O" addr field
        | ArrayLength(addr, dim, _) -> sprintf "Length(%O, %O)" addr dim
        | BoxedLocation(addr, typ) -> sprintf "%O^%s" typ (addr |> List.map toString |> join ".")
        | StackBufferIndex(key, idx) -> sprintf "%O[%O]" key idx
        | ArrayLowerBound(addr, dim, _) -> sprintf "LowerBound(%O, %O)" addr dim
    member x.Zone() =
        match x with
        | PrimitiveStackLocation _
        | StackBufferIndex _ -> "Stack"
        | ClassField _
        | ArrayIndex _
        | ArrayLength _
        | BoxedLocation _
        | ArrayLowerBound  _ -> "Heap"
        | StaticField _ -> "Statics"
        | StructField(addr, _) -> addr.Zone()

and
    [<CustomEquality;NoComparison>]
    term =
        {term : termNode; hc : int}
        override x.ToString() = x.term.ToString()
        override x.GetHashCode() = x.hc
        override x.Equals(o : obj) =
            match o with
            | :? term as other -> LanguagePrimitives.PhysicalEquality x.term other.term
            | _ -> false

and
    ISymbolicConstantSource =
        abstract SubTerms : term seq
        abstract Time : vectorTime
        abstract TypeOfLocation : Type

type INonComposableSymbolicConstantSource =
    inherit ISymbolicConstantSource

module HashMap =
    let hashMap = weakdict<termNode, term>()
    let addTerm node =
        let result = ref { term = node; hc = 0 }
        if hashMap.TryGetValue(node, result)
            then result.Value
            else
                let hc = hash node
                let term = { term = node; hc = hc }
                hashMap.Add(node, term)
                term

[<AutoOpen>]
module internal Terms =

    let term (term : term) = term.term

// --------------------------------------- Primitives ---------------------------------------

    let Nop = HashMap.addTerm Nop
    let Concrete obj typ = HashMap.addTerm (Concrete(obj, typ))
    let Constant name source typ = HashMap.addTerm (Constant({v=name}, source, typ))
    let Expression op args typ = HashMap.addTerm (Expression(op, args, typ))
    let Struct fields typ = HashMap.addTerm (Struct(fields, typ))
    let HeapRef address baseType =
        match address.term with
        | Constant(name, _, _) when name.v = "this.head" -> ()
        | _ -> ()
        HashMap.addTerm (HeapRef(address, baseType))
    let Ref address =
        match address with
        | ArrayIndex(_, indices, (_, dim, _)) -> assert(List.length indices = dim)
        | _ -> ()
        HashMap.addTerm (Ref address)
    let Ptr baseAddress typ offset = HashMap.addTerm (Ptr(baseAddress, typ, offset))
    let Slice term first termSize pos = HashMap.addTerm (Slice(term, first, termSize, pos))
    let ConcreteHeapAddress addr = Concrete addr addressType
    let Union gvs =
        if List.length gvs < 2 then internalfail "Empty and one-element unions are forbidden!"
        HashMap.addTerm (Union gvs)

    let isVoid = term >> function
        | Nop -> true
        | _ -> false

    let isConcrete = term >> function
        | Concrete _ -> true
        | _ -> false

    let isUnion = term >> function
        | Union _ -> true
        | _ -> false

    let isTrue = term >> function
        | Concrete(b, Bool) when (b :?> bool) -> true
        | _ -> false

    let isFalse = term >> function
        | Concrete(b, Bool) when not (b :?> bool) -> true
        | _ -> false

    let operationOf = term >> function
        | Expression(op, _, _) -> op
        | term -> internalfailf "expression expected, %O received" term

    let argumentsOf = term >> function
        | Expression(_, args, _) -> args
        | term -> internalfailf "expression expected, %O received" term

    let fieldsOf = term >> function
        | Struct(fields, _) -> fields
        | term -> internalfailf "struct or class expected, %O received" term

    let private typeOfUnion getType gvs =
        let types = List.map (snd >> getType) gvs
        match types with
        | [] -> __unreachable__()
        | t::ts ->
            let allSame =
                List.forall ((=) t) ts
            if allSame then t
            else internalfailf "evaluating type of unexpected union %O!" gvs

    let commonTypeOf getType term =
        match term.term with
        | Nop -> typeof<System.Void>
        | Union gvs -> typeOfUnion getType gvs
        | _ -> getType term

    let typeOfAddress = function
        | ClassField(_, field)
        | StructField(_, field)
        | StaticField(_, field) -> field.typ
        | ArrayIndex(_, _, (elementType, _, _)) -> elementType
        | BoxedLocation(_, typ) -> typ
        | ArrayLength _
        | ArrayLowerBound  _ -> lengthType
        | StackBufferIndex _ -> typeof<int8>
        | PrimitiveStackLocation loc -> loc.TypeOfLocation

    let typeOfRef =
        let getTypeOfRef = term >> function
            | HeapRef(_, t) -> t
            | Ref address -> typeOfAddress address
            | Ptr(_, sightType, _) -> sightType
            | term -> internalfailf "expected reference, but got %O" term
        commonTypeOf getTypeOfRef

    let sightTypeOfPtr =
        let getTypeOfPtr = term >> function
            | Ptr(_, typ, _) -> typ
            | term -> internalfailf "expected pointer, but got %O" term
        commonTypeOf getTypeOfPtr

    let baseTypeOfPtr = typeOfRef

    let typeOf =
        let getType term =
            match term.term with
            | Concrete(_, t)
            | Constant(_, _, t)
            | Expression(_, _, t)
            | HeapRef(_, t)
            | Struct(_, t) -> t
            | Ref _ -> (typeOfRef term).MakeByRefType()
            | Ptr _ -> (sightTypeOfPtr term).MakePointerType()
            | _ -> internalfailf "getting type of unexpected term %O" term
        commonTypeOf getType

    let symbolicTypeToArrayType = function
        | ArrayType(elementType, dim) ->
            match dim with
            | Vector -> (elementType, 1, true)
            | ConcreteDimension d -> (elementType, d, false)
            | SymbolicDimension -> __insufficientInformation__ "Cannot process array of unknown dimension!"
        | typ -> internalfailf "symbolicTypeToArrayType: expected array type, but got %O" typ

    let arrayTypeToSymbolicType (elemType : Type, dim, isVector) =
        if isVector then elemType.MakeArrayType()
        else elemType.MakeArrayType(dim)

    let sizeOf = typeOf >> internalSizeOf

    let bitSizeOf term resultingType = bitSizeOfType (typeOf term) resultingType

    let isBool t =    typeOf t = typeof<bool>
    let isNumeric t = typeOf t |> isNumeric

    let rec isStruct term =
        match term.term with
        | Struct _ -> true
        | Union gvs -> List.forall (snd >> isStruct) gvs
        | _ -> false

    let rec isReference term =
        match term.term with
        | HeapRef _ -> true
        | Ref _ -> true
        | Union gvs -> List.forall (snd >> isReference) gvs
        | _ -> false

    let isPtr = term >> function
        | Ptr _ -> true
        | _ -> false

    // Only for concretes: there will never be null type
    let canCastConcrete (concrete : obj) targetType =
        let actualType = getTypeOfConcrete concrete
        actualType = targetType || targetType.IsAssignableFrom(actualType)

    let castConcrete (concrete : obj) (t : Type) =
        let actualType = getTypeOfConcrete concrete
        let functionIsCastedToMethodPointer () =
            typedefof<System.Reflection.MethodBase>.IsAssignableFrom(actualType) && typedefof<IntPtr>.IsAssignableFrom(t)
        if actualType = t then
            Concrete concrete t
        elif actualType = typeof<int> && t = typeof<IntPtr> then
            Concrete (IntPtr(concrete :?> int)) t
        elif actualType = typeof<uint> && t = typeof<UIntPtr> then
            Concrete (UIntPtr(concrete :?> uint)) t
        elif t.IsEnum && TypeUtils.isNumeric actualType then
            let underlyingType = getEnumUnderlyingTypeChecked t
            let underlyingValue = convert concrete underlyingType
            let enumValue = convert underlyingValue t
            Concrete enumValue t
        elif actualType.IsEnum && TypeUtils.isNumeric t then
            try
                Concrete (Convert.ChangeType(concrete, t)) t
            with :? OverflowException ->
                Concrete concrete t
        elif canConvert actualType t then
            Concrete (convert concrete t) t
        elif t.IsAssignableFrom(actualType) then
            Concrete concrete t
        elif functionIsCastedToMethodPointer() then
            Concrete concrete actualType
        else raise(InvalidCastException(sprintf "Cannot cast %s to %s!" (Reflection.getFullTypeName actualType) (Reflection.getFullTypeName t)))

    let True =
        Concrete (box true) typeof<bool>

    let False =
        Concrete (box false) typeof<bool>

    let makeBool predicate =
        if predicate then True else False

    let makeIndex (i : int) =
        Concrete i indexType

    let zeroAddress =
        Concrete VectorTime.zero addressType

    let nullRef t =
        HeapRef zeroAddress t

    let makeNumber n =
        Concrete n (n.GetType())

    let makeNullPtr typ =
        Ptr (HeapLocation(zeroAddress, typ)) typ (makeNumber 0)

    let makeBinary operation x y t =
        assert(Operations.isBinary operation)
        Expression (Operator operation) [x; y] t

    let makeNAry operation x t =
        match x with
        | [] -> raise(ArgumentException("List of args should be not empty"))
        | [x] -> x
        | _ -> Expression (Operator operation) x t

    let makeUnary operation x t =
        assert(Operations.isUnary operation)
        Expression (Operator operation) [x] t

    let (|CastExpr|_|) = term >> function
        | Expression(Cast(srcType, dstType), [x], t) ->
            assert(dstType = t)
            Some(CastExpr(x, srcType, dstType))
        | _ -> None

    let rec private makeCast term fromType toType =
        match term, toType with
        | _ when fromType = toType -> term
        | CastExpr(x, xType, Numeric t), Numeric dstType when not <| isLessForNumericTypes t dstType ->
            makeCast x xType toType
        | CastExpr(x, (Numeric srcType as xType), Numeric t), Numeric dstType
            when not <| isLessForNumericTypes t srcType && not <| isLessForNumericTypes dstType t ->
            makeCast x xType toType
        | _ -> Expression (Cast(fromType, toType)) [term] toType

    let rec primitiveCast term targetType =
        match term.term, targetType with
        | _ when typeOf term = targetType -> term
        // NOTE: Lazy: not casting numbers to ref or ptr until dereference
        | _, Pointer _
        | _, ByRef _ -> term
        | Concrete(value, _), _ -> castConcrete value targetType
        // TODO: make cast to Bool like function Transform2BooleanTerm
        | Constant(_, _, t), _
        | Expression(_, _, t), _ -> makeCast term t targetType
        | Union gvs, _ -> gvs |> List.map (fun (g, v) -> (g, primitiveCast v targetType)) |> Union
        | _ -> __unreachable__()

    let negate term =
        assert(isBool term)
        makeUnary OperationType.LogicalNot term typeof<bool>

    let (|True|_|) term = if isTrue term then Some True else None
    let (|False|_|) term = if isFalse term then Some False else None

    let (|ConcreteT|_|) = term >> function
        | Concrete(name, typ) -> Some(ConcreteT(name, typ))
        | _ -> None

    let (|UnionT|_|) = term >> function
        | Union gvs -> Some(UnionT gvs)
        | _ -> None

    let (|GuardedValues|_|) = function // TODO: this could be ineffective (because of unzip)
        | Union gvs -> Some(GuardedValues(List.unzip gvs))
        | _ -> None

    let (|UnaryMinus|_|) = function
        | Expression(Operator OperationType.UnaryMinus, [x], t) -> Some(UnaryMinus(x, t))
        | _ -> None

    let (|UnaryMinusT|_|) = term >> (|UnaryMinus|_|)

    let (|Add|_|) = term >> function
        | Expression(Operator OperationType.Add, [x;y], t) -> Some(Add(x, y, t))
        | _ -> None

    let (|Sub|_|) = term >> function
        | Expression(Operator OperationType.Subtract, [x;y], t) -> Some(Sub(x, y, t))
        | _ -> None

    let (|Mul|_|) = term >> function
        | Expression(Operator OperationType.Multiply, [x;y], t) -> Some(Mul(x, y, t))
        | _ -> None

    let (|Div|_|) = term >> function
        | Expression(Operator OperationType.Divide, [x;y], t) -> Some(Div(x, y, t, true))
        | Expression(Operator OperationType.Divide_Un, [x;y], t) -> Some(Div(x, y, t, false))
        | _ -> None

    let (|Rem|_|) = term >> function
        | Expression(Operator OperationType.Remainder, [x;y], t)
        | Expression(Operator OperationType.Remainder_Un, [x;y], t) -> Some(Rem(x, y, t))
        | _ -> None

    let (|Negation|_|) = function
        | Expression(Operator OperationType.LogicalNot, [x], _) -> Some(Negation x)
        | _ -> None

    let (|NegationT|_|) = term >> (|Negation|_|)

    let (|Conjunction|_|) = function
        | Expression(Operator OperationType.LogicalAnd, xs, _) -> Some(Conjunction xs)
        | _ -> None

    let (|Disjunction|_|) = function
        | Expression(Operator OperationType.LogicalOr, xs, _) -> Some(Disjunction xs)
        | _ -> None

    let (|Xor|_|) = term >> function
        | Expression(Operator OperationType.LogicalXor, [x;y], _) -> Some(Xor(x, y))
        | _ -> None

    let (|ShiftLeft|_|) = term >> function
        | Expression(Operator OperationType.ShiftLeft, [x;y], t) -> Some(ShiftLeft(x, y, t))
        | _ -> None

    let (|ShiftRight|_|) = term >> function
        | Expression(Operator OperationType.ShiftRight, [x;y], t) -> Some(ShiftRight(x, y, t, true))
        | Expression(Operator OperationType.ShiftRight_Un, [x;y], t) -> Some(ShiftRight(x, y, t, false))
        | _ -> None

    let (|ShiftRightThroughCast|_|) = function
        | CastExpr(ShiftRight(a, b, Numeric t, _), _, (Numeric castType as t')) when not <| isLessForNumericTypes castType t ->
            Some(ShiftRightThroughCast(primitiveCast a t', b, t'))
        | _ -> None

    let (|Combined|_|) = term >> function
        | Expression(Combine, args, t) -> Some(Combined(args, t))
        | _ -> None

    let (|ConcreteHeapAddress|_|) = function
        | Concrete(:? concreteHeapAddress as a, AddressType) -> ConcreteHeapAddress a |> Some
        | _ -> None

    let getConcreteHeapAddress = term >> function
        | ConcreteHeapAddress(addr) -> addr
        | _ -> __unreachable__()

    let tryPtrToArrayInfo (typeOfBase : Type) sightType offset =
        match offset.term with
        | Concrete(:? int as offset, _) ->
            let checkType() =
                typeOfBase.IsSZArray && typeOfBase.GetElementType() = sightType
                || typeOfBase = typeof<string> && sightType = typeof<char>
            let mutable elemSize = 0
            let checkOffset() =
                elemSize <- internalSizeOf sightType
                (offset % elemSize) = 0
            if not typeOfBase.ContainsGenericParameters && checkType() && checkOffset() then
                let arrayType = (sightType, 1, true)
                let index = int (offset / elemSize)
                Some ([makeNumber index], arrayType)
            else None
        | _ -> None

    let tryIntListFromTermList (termList : term list) =
        let addElement term concreteList k =
            match term.term with
            | Concrete(:? int16 as i, _) -> int i :: concreteList |> k
            | Concrete(:? uint16 as i, _) -> int i :: concreteList |> k
            | Concrete(:? char as i, _) -> int i :: concreteList |> k
            | Concrete(:? int as i, _) -> i :: concreteList |> k
            | Concrete(:? uint as i, _) -> int i :: concreteList |> k
            | Concrete(:? int64 as i, _) -> int i :: concreteList |> k
            | Concrete(:? uint64 as i, _) -> int i :: concreteList |> k
            | _ -> None
        Cps.List.foldrk addElement List.empty termList Some

    let isConcreteHeapAddress term =
        match term.term with
        | ConcreteHeapAddress _ -> true
        | _ -> false

    let rec private concreteToBytes (obj : obj) =
        match obj with
        | _ when obj = null -> internalSizeOf typeof<obj> |> Array.zeroCreate
        | :? byte as o -> Array.singleton o
        | :? sbyte as o -> Array.singleton (byte o)
        | :? int16 as o -> BitConverter.GetBytes o
        | :? uint16 as o -> BitConverter.GetBytes o
        | :? int as o -> BitConverter.GetBytes o
        | :? uint32 as o -> BitConverter.GetBytes o
        | :? int64 as o -> BitConverter.GetBytes o
        | :? uint64 as o -> BitConverter.GetBytes o
        | :? float32 as o -> BitConverter.GetBytes o
        | :? double as o -> BitConverter.GetBytes o
        | :? bool as o -> BitConverter.GetBytes o
        | :? char as o -> BitConverter.GetBytes o
        | _ when obj.GetType().IsEnum ->
            let i = Convert.ChangeType(obj, getEnumUnderlyingTypeChecked (obj.GetType()))
            concreteToBytes i
        | _ -> internalfailf "getting bytes from concrete: unexpected obj %O" obj

    let rec private bytesToObj (bytes : byte[]) t =
        let span = ReadOnlySpan<byte>(bytes)
        match t with
        | _ when t = typeof<byte> -> Array.head bytes :> obj
        | _ when t = typeof<sbyte> -> sbyte (Array.head bytes) :> obj
        | _ when t = typeof<int16> -> BitConverter.ToInt16 span :> obj
        | _ when t = typeof<uint16> -> BitConverter.ToUInt16 span :> obj
        | _ when t = typeof<int> -> BitConverter.ToInt32 span :> obj
        | _ when t = typeof<uint32> -> BitConverter.ToUInt32 span :> obj
        | _ when t = typeof<int64> -> BitConverter.ToInt64 span :> obj
        | _ when t = typeof<uint64> -> BitConverter.ToUInt64 span :> obj
        | _ when t = typeof<float32> -> BitConverter.ToSingle span :> obj
        | _ when t = typeof<double> -> BitConverter.ToDouble span :> obj
        | _ when t = typeof<bool> -> BitConverter.ToBoolean span :> obj
        | _ when t = typeof<char> -> BitConverter.ToChar span :> obj
        | _ when t.IsEnum ->
            let i = getEnumUnderlyingTypeChecked t |> bytesToObj bytes
            Enum.ToObject(t, i)
        | _ -> internalfailf "creating object from bytes: unexpected object type %O" t

    let rec reinterpretConcretes (sliceTerms : term list) t =
        let bytes : byte array = internalSizeOf t |> Array.zeroCreate
        let folder bytes slice =
            match slice.term with
            | Slice(term, {term = Concrete(s, _)}, {term = Concrete(e, _)}, {term = Concrete(pos, _)}) ->
                let s = convert s typeof<int> :?> int
                let e = convert e typeof<int> :?> int
                let pos = convert pos typeof<int> :?> int
                let o = slicingTerm term
                let sliceBytes = concreteToBytes o
                let sliceStart = max s 0
                // NOTE: 'pos = 0' is write case
                let termStart = if pos = 0 then max s 0 else max (-s) 0
                let e = min e (Array.length sliceBytes)
                let count = e - sliceStart
                if count > 0 then Array.blit sliceBytes sliceStart bytes termStart count
                bytes
            | _ -> internalfailf "expected concrete slice, but got %O" slice
        let bytes = List.fold folder bytes sliceTerms
        bytesToObj bytes t

    and private slicingTerm term =
        match term with
        | {term = Concrete(o, _)} -> o
        | Combined(slices, t) -> reinterpretConcretes slices t
        | _ -> internalfail "getting slicing term: unexpected term %O" term

    let rec private allSlicesAreConcrete slices =
        let rec sliceIsConcrete = function
            | {term = Slice({term = Concrete _}, {term = Concrete _}, {term = Concrete _}, {term = Concrete _})} -> true
            | Combined(slices, _) -> allSlicesAreConcrete slices
            | _ -> false
        List.forall sliceIsConcrete slices

    let combine terms t =
        let defaultCase() = Expression Combine terms t
        assert(List.isEmpty terms |> not)
        match terms with
        | _ when allSlicesAreConcrete terms -> Concrete (reinterpretConcretes terms t) t
        | [{term = Slice(t, {term = Concrete(:? int as s, _)}, {term = Concrete(:? int as e, _)}, _)}] when s = 0 && e = sizeOf t -> t
        | [{term = Slice _ }] -> defaultCase()
        | [nonSliceTerm] -> nonSliceTerm
        | _ -> defaultCase()

    let rec timeOf (address : heapAddress) =
        match address.term with
        | ConcreteHeapAddress addr -> addr
        | Constant(_, source, _) -> source.Time
        | HeapRef(address, _) -> timeOf address
        | Union gvs -> List.fold (fun m (_, v) -> VectorTime.max m (timeOf v)) VectorTime.zero gvs
        | _ -> internalfailf "timeOf : expected heap address, but got %O" address

    let compareTerms t1 t2 =
        match t1.term, t2.term with
        | Concrete(:? IComparable as x, _), Concrete(:? IComparable as y, _) -> x.CompareTo y
        | Concrete(:? IComparable, _), _ -> -1
        | _, Concrete(:? IComparable, _) -> 1
        | _ -> compare (toString t1) (toString t2)

    let rec private foldChildren folder state term k =
        match term.term with
        | Constant(_, source, _) ->
            foldSeq folder source.SubTerms state k
        | Expression(_, args, _) ->
            foldSeq folder args state k
        | Struct(fields, _) ->
            foldSeq folder (PersistentDict.values fields) state k
        | Ref address ->
            foldAddress folder state address k
        | Ptr(address, _, indent) ->
            foldPointerBase folder state address (fun state ->
            doFold folder state indent k)
        | GuardedValues(gs, vs) ->
            foldSeq folder gs state (fun state ->
            foldSeq folder vs state k)
        | Slice(t, s, e, pos) ->
            doFold folder state t (fun state ->
            doFold folder state s (fun state ->
            doFold folder state e (fun state ->
            doFold folder state pos k)))
        | _ -> k state

    and doFold folder state term k =
        folder state term k (fun state ->
        foldChildren folder state term k)

    and foldAddress folder state address k =
        match address with
        | PrimitiveStackLocation _
        | StaticField _
        | BoxedLocation _ -> k state
        | ClassField(addr, _) -> doFold folder state addr k
        | ArrayIndex(addr, idcs, _) ->
            doFold folder state addr (fun state ->
            foldSeq folder idcs state k)
        | StructField(addr, _) -> foldAddress folder state addr k
        | ArrayLength(addr, idx, _)
        | ArrayLowerBound(addr, idx, _) ->
            doFold folder state addr (fun state ->
            doFold folder state idx k)
        | StackBufferIndex(_, idx) -> doFold folder state idx k

    and foldPointerBase folder state pointerBase k =
        match pointerBase with
        | HeapLocation(heapAddress, _) -> doFold folder state heapAddress k
        | StackLocation _
        | StaticLocation _ -> k state

    and private foldSeq folder terms state k =
        Cps.Seq.foldlk (doFold folder) state terms k

    let fold folder state terms =
        foldSeq folder terms state id

    let iter action term =
        doFold action () term id

    let iterSeq action terms =
        Cps.Seq.foldlk (doFold action) () terms id

    let discoverConstants terms =
        let result = HashSet<term>()
        let addConstant _ t _ into =
            match t.term with
            | Constant _ -> result.Add t |> ignore |> into
            | _ -> into ()
        Seq.iter (iter addConstant) terms
        result :> ISet<term>

    let private foldFields isStatic folder acc typ =
        let fields = Reflection.fieldsOf isStatic typ
        let addField heap (fieldId, fieldInfo : Reflection.FieldInfo) =
            folder heap fieldInfo fieldId fieldInfo.FieldType
        FSharp.Collections.Array.fold addField acc fields

    let private makeFields isStatic makeField typ =
        let folder fields fieldInfo field termType =
            let value = makeField fieldInfo field termType
            PersistentDict.add field value fields
        foldFields isStatic folder PersistentDict.empty typ

    let makeStruct isStatic makeField typ =
        let fields = makeFields isStatic makeField typ
        Struct fields typ

    let rec makeDefaultValue typ =
        match typ with
        | Bool -> False
        | Numeric t when t.IsEnum -> castConcrete (getEnumDefaultValue t) t
        // NOTE: XML serializer does not support special char symbols, so creating test with char > 32 #XMLChar
        // TODO: change serializer
        | Numeric t when t = typeof<char> -> makeNumber (char 33)
        | Numeric t -> castConcrete 0 t
        | _ when typ = typeof<IntPtr> -> castConcrete 0 typ
        | _ when typ = typeof<UIntPtr> -> castConcrete 0u typ
        | ByRef _
        | ArrayType _
        | ClassType _
        | InterfaceType _ -> nullRef typ
        | TypeVariable t when isReferenceTypeParameter t -> nullRef typ
        | TypeVariable t -> __insufficientInformation__ "Cannot instantiate value of undefined type %O" t
        | StructType _ -> makeStruct false (fun _ _ t -> makeDefaultValue t) typ
        | Pointer typ -> makeNullPtr typ
        | AddressType -> zeroAddress
        | _ -> __notImplemented__()
