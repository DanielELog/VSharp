using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VSharp.Test;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Tests.MaybeTests;

namespace IntegrationTests;


[TestSvmFixture]
public static class FSharpExt
{
    private class MyClass
    {
        public override string ToString()
        {
            return "My custom class";
        }
    }

    [TestSvm]
    public static bool Test1()
    {
        Maybe<MyClass> maybe = null;

        bool result = maybe.HasValue;
        result = !result && maybe.HasNoValue;
        return result;
    }
    [TestSvm]
    public static bool Test2()
    {
        bool compareUsingMaybeDynamic = EqualityComparer<Maybe<dynamic>>.Default.Equals(Maybe<dynamic>.None, Maybe<dynamic>.None);
        bool compareObjectNotDynamic = EqualityComparer<object>.Default.Equals(Maybe<int>.None, Maybe<int>.None);
        // bool compareDynamicInt = EqualityComparer<object>.Default.Equals((dynamic)1, (dynamic)1);
        //NSubstitute is using object as generic type which doesnt work for Maybe<dynamic>
        bool compareUsingMaybeObject = EqualityComparer<object>.Default.Equals(Maybe<dynamic>.None, Maybe<dynamic>.None);
        // bool equalsDynamic1 = Maybe<dynamic>.None == (dynamic)Maybe<dynamic>.None;
        // bool equalsDynamic2 = Maybe<dynamic>.None.Equals((dynamic)Maybe<dynamic>.None);
        bool equalsDynamic3 = Maybe<object>.None == (object)new Maybe<object>();
        return compareUsingMaybeDynamic && compareObjectNotDynamic &&
               // compareDynamicInt &&
               compareUsingMaybeObject &&
               // equalsDynamic1 &&
               // equalsDynamic2 &&
               equalsDynamic3;
    }
    [TestSvm]
    public static bool Test3()
    {
        bool compareMaybeIntUsingObject = EqualityComparer<object>.Default.Equals(Activator.CreateInstance(typeof(Maybe<int>)), Activator.CreateInstance(typeof(Maybe<int>)));
        bool compareUsingMaybeDynamic = EqualityComparer<Maybe<dynamic>>.Default.Equals(Activator.CreateInstance(typeof(Maybe<dynamic>)), Activator.CreateInstance(typeof(Maybe<dynamic>)));
        //NSubstitute is using object as generic type which doesnt work for Maybe<dynamic>
        bool compareMaybeDynamicUsingObject = EqualityComparer<object>.Default.Equals(Activator.CreateInstance(typeof(Maybe<dynamic>)), Activator.CreateInstance(typeof(Maybe<dynamic>)));
        return compareUsingMaybeDynamic && compareMaybeIntUsingObject && compareMaybeDynamicUsingObject;
    }

    public static Task<T> AsTask<T>(this T obj) => Task.FromResult(obj);
    public static Task AsTask(this Exception exception) => Task.FromException(exception);
    public static Task<T> AsTask<T>(this Exception exception) => Task.FromException<T>(exception);

    private static Task<Maybe<MyClass>> GetMaybeTask(Maybe<MyClass> maybe) => maybe.AsTask();

    [TestSvm]
    public static bool Test4()
    {
        var instance = new MyClass();
        Maybe<MyClass> maybe1 = instance;
        Maybe<MyClass> maybe2 = instance;

        bool equals1 = maybe1.Equals(maybe2);
        bool equals2 = ((object)maybe1).Equals(maybe2);
        bool equals3 = maybe1 == maybe2;
        bool equals4 = maybe1 != maybe2;
        bool equals5 = maybe1.GetHashCode() == maybe2.GetHashCode();
        return equals1 && equals2 && equals3 && equals4 && equals5;
    }

    [TestSvm]
    public static bool Test5()
    {
        var instance = new MyClass();
        Maybe<MyClass> maybe = instance;

        MyClass myClass = maybe.GetValueOrDefault();

        return myClass == instance;
    }

    [TestSvm]
    public static bool Test6()
    {
        ImplicitConversionTests.StringVO stringVo = default;
        // ReSharper disable once ExpressionIsAlwaysNull
        string stringPrimitive = stringVo;
        ImplicitConversionTests.IntVO intVo = default;
        // ReSharper disable once ExpressionIsAlwaysNull
        int intPrimitive = intVo;
        return stringPrimitive == null && intPrimitive == 0;
    }

    public static async Task<bool> ToResult_returns_failure_if_has_no_value()
    {
        var maybeTask = GetMaybeTask(Maybe<MyClass>.None);

        Result<MyClass> result = await maybeTask.ToResult("Error");

        var a = result.IsSuccess;
        var b = result.Error == "Error";
        return a && b;
    }

    public static async Task<bool> True()
    {
        return true;
    }

    public static async Task<bool> False()
    {
        return false;
    }

    public static async Task<bool> AsyncTask(int n)
    {
        Task<bool> trueTask = True();
        Task<bool> falseTask = False();

        return await trueTask && !await falseTask && n == 0;
    }

    [TestSvm]
    public static bool Test7()
    {
        return AsyncTask(0).Result;
    }

    [TestSvm]
    public static bool Test8(int n)
    {
        return AsyncTask(n).Result;
    }

    public class T
    {
        public static readonly T Value = new T();

        public static readonly T Value2 = new T();
    }

    public static bool actionExecuted;
    public static Result FailedResult => Result.Failure(ErrorMessage);
    public static Result<T> FailedResultT => Result.Failure<T>(ErrorMessage);

    public static Result GetResult(bool isSuccess)
    {
        actionExecuted = true;
        return isSuccess
            ? Result.Success()
            : FailedResult;
    }

    public const string ErrorMessage = "Error Message";

    [TestSvm]
    public static bool Test7(bool resultSuccess, bool funcSuccess)
    {
        Result<T> result = Result.SuccessIf(resultSuccess, T.Value, ErrorMessage);

        var returned = result.Check(_ => GetResult(funcSuccess));
        var a = funcSuccess ? result : FailedResultT;

        return actionExecuted == resultSuccess && returned.Equals(a);
    }


}
