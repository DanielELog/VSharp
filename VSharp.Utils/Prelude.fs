﻿namespace VSharp
open VSharp.CSharpUtils

type InternalException (msg : string) = inherit System.Exception(msg)
exception UnreachableException of string

[<AutoOpen>]
module public Prelude =
//    let public internalfail message = "Internal error: " + message |> failwith
    let inline public internalfail message = raise (InternalException <| "Internal error: " + message)
    let inline public internalfailf format = Printf.ksprintf internalfail format
    let inline public __notImplemented__() = raise (new System.NotImplementedException())
    let inline public __unreachable__() = raise (UnreachableException "unreachable branch hit!") //internalfail "unreachable branch hit!"

    let inline public toString x = x.ToString()
    let inline public join s (ss : seq<string>) = System.String.Join(s, ss)

    let public always x _ = x

    let public eval x = x ()

    let inline public cons x xs = x :: xs
    let inline public optCons xs = function
        | Some x -> x::xs
        | None -> xs

    let inline public withFst x = fun y -> (x, y)
    let inline public withSnd y = fun x -> (x, y)
    let inline public makePair x y = (x, y)

    let public mapfst f (x, y) = (f x, y)
    let public mapsnd f (x, y) = (x, f y)

    let inline public fst3 (x, _, _) = x
    let inline public snd3 (_, y, _) = y
    let inline public thd3 (_, _, z) = z

    let inline public appIfNotNull f lhs rhs =
        if lhs = null then rhs else f lhs

    let inline public (|?) lhs rhs =
        if lhs = null then rhs else lhs
    let inline public (|??) lhs rhs = Option.defaultValue rhs lhs
    let inline public (||??) (lhs : 'a option) (rhs : 'a Lazy) = Option.defaultWith rhs.Force lhs

    let safeGenericTypeDefinition (t : System.Type) =
        if t.IsGenericType && not t.IsGenericTypeDefinition then t.GetGenericTypeDefinition() else t

[<CustomEquality;NoComparison>]
type 'a transparent =
    { v : 'a }
    override x.ToString() = x.v.ToString()
    override x.GetHashCode() = x.GetType().GetDeterministicHashCode()
    override x.Equals(o : obj) =
        o :? 'a transparent

type [<CustomEquality;NoComparison>] fieldId =
    | FieldId of string
    override x.GetHashCode() =
        match x with
        | FieldId s -> s.GetDeterministicHashCode()
    override x.Equals(other) =
        match other with
        | :? fieldId as other -> hash x = hash other
        | _ -> false
    override x.ToString() = match x with FieldId s -> s

type 'a symbolicValue =
    | Specified of 'a
    | Unspecified
