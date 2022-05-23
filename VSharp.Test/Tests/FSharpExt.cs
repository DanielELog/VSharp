using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VSharp.Test;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Json.Serialization;
using CSharpFunctionalExtensions.Tests.MaybeTests;
using NUnit.Framework;

namespace IntegrationTests;


[TestSvmFixture]
public static class FSharpExt
{
    private class MyClass
    {
        public int x;
        public override string ToString()
        {
            return "My custom class";
        }
    }

    [TestSvm(100)]
    public static bool Test1()
    {
        Maybe<MyClass> maybe = null;

        bool result = maybe.HasValue;
        result = !result && maybe.HasNoValue;
        return result;
    }
    [TestSvm(100)]
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
    [TestSvm(100)]
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

    [TestSvm(100)]
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

    [TestSvm(100)]
    public static bool Test5()
    {
        var instance = new MyClass();
        Maybe<MyClass> maybe = instance;

        MyClass myClass = maybe.GetValueOrDefault();

        return myClass == instance;
    }

    [TestSvm(100)]
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

        return await trueTask && !await falseTask && n == 128;
    }

    [TestSvm(100)]
    public static bool ConcreteAsyncAwait()
    {
        return AsyncTask(0).Result;
    }

    [TestSvm(100)]
    public static bool SymbolicAsyncAwait(int n)
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

    [TestSvm(100)]
    public static bool Test7(bool resultSuccess, bool funcSuccess)
    {
        Result<T> result = Result.SuccessIf(resultSuccess, T.Value, ErrorMessage);

        var returned = result.Check(_ => GetResult(funcSuccess));
        var a = funcSuccess ? result : FailedResultT;

        return actionExecuted == resultSuccess && returned.Equals(a);
    }

    public static async Task<string> AsyncHttp()
    {
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        byte[] a = new byte[] { 1 };
        httpResponseMessage.Content = new ByteArrayContent(a);
        return await httpResponseMessage.Content.ReadAsStringAsync();
    }

    public static async Task<bool> AsyncHttp2()
    {
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        return await httpResponseMessage.IsSuccessStatusCode.AsTask();
    }

    [Ignore("not ready")]
    public static bool HttpResponseMessage()
    {
        return AsyncHttp().Result == "Great Success";
    }

    [TestSvm(100)]
    public static bool HttpResponseMessage2()
    {
        return AsyncHttp2().Result;
    }

    [TestSvm(100)]
    public static bool InterpolatedString()
    {
        const string value = "Great Success";
        return $"{value}" == "Great Success";
    }

    [TestSvm(100)]
    public static bool InterpolatedString2()
    {
        const string value = "Great Success";
        string s = $"{{ \"Error\": null, \"Value\": \"{value}\"}}";
        return s.Contains(value);
    }

    [TestSvm(100)]
    public static bool InterpolatedString3(int n)
    {
        string value = "Great Success" + n;
        string s = $"{{ \"Error\": null, \"Value\": \"{value}\"}}";
        return s.Contains(value);
    }

    [TestSvm(100)]
    public static bool InterpolatedString4(int n)
    {
        string value = "Great Success";
        string s = $"{{ \"Error\": null, \"Value\": \"{value}\"}}";
        return s.Contains(value + n);
    }

    [TestSvm(100)]
    public static bool StringBuilder1(int n)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Great");
        stringBuilder.AppendLine(" Success");
        return stringBuilder.ToString().Contains("Great Success");
    }

    [TestSvm(100)]
    public static bool StringBuilder2(int n)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Great");
        stringBuilder.AppendLine(" Success");
        stringBuilder.Append(n);
        return stringBuilder.ToString().Contains("Great Success");
    }

    [TestSvm(100)]
    public static bool StringBuilder3(int n)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Great");
        stringBuilder.Append(n);
        stringBuilder.AppendLine(" Success");
        return stringBuilder.ToString().Contains($"Great{n} Success");
    }

    [Ignore("not ready")]
    public static bool FileRead()
    {
        string text = System.IO.File.ReadAllText(@"../newfile.txt");
        return text == "ab";
    }

    [Ignore("not ready")]
    public static bool FileExists()
    {
        return System.IO.File.Exists(@"../newfile.txt");
    }

    [Ignore("not ready")]
    public static bool JsonSerialize()
    {
        Maybe<int> a = Maybe<int>.None;
        string jsonString = JsonSerializer.Serialize(a);
        return JsonSerializer.Deserialize<Maybe<int>>(jsonString) == a;
    }

    [TestSvm(100)]
    public static bool ResultSuccessIf()
    {
        Result result = Result.SuccessIf(true, 7, null);
        return result.IsSuccess;
    }

    [TestSvm(100)]
    public static bool ResultSuccessIf2(int n)
    {
        Result result = Result.SuccessIf(true, n, null);
        return result.IsSuccess;
    }

    public static async Task<Result> TaskOfResult(StringBuilder stringBuilder, string s, bool success = true)
    {
        await Task.Yield();
        if (success)
        {
            stringBuilder.Append(s);
        }
        return success ? Result.Success() : Result.Failure(s);
    }

    public static async Task<bool> CombineInOrder()
    {
        StringBuilder builder = new StringBuilder();
        IEnumerable<Task<Result>> tasks =
            new [] { "a", "b", "c" }
                .Select( s => TaskOfResult(builder, s));

        Result result = await tasks.CombineInOrder(";");

        return result.IsSuccess && builder.ToString() == "abc";
    }

    [Ignore("not ready")]
    public static bool CombineInOrderTest()
    {
        return CombineInOrder().Result;
    }

    [TestSvm(100)]
    public static bool DestructSuccess()
    {
        var (isSuccess, isFailure, value) = Result.Success(100);

        return isSuccess && !isFailure && value == 100;
    }

    [TestSvm(100)]
    public static bool DestructSuccess1(int n)
    {
        var (isSuccess, isFailure, value) = Result.Success(n);

        return isSuccess && !isFailure && value == n;
    }

    private static Stream Serialize(object source)
    {
        IFormatter formatter = new BinaryFormatter();
        Stream stream = new MemoryStream();
        formatter.Serialize(stream, source);
        return stream;
    }

    private static T Deserialize<T>(Stream stream)
    {
        IFormatter formatter = new BinaryFormatter();
        stream.Position = 0;
        return (T)formatter.Deserialize(stream);
    }

    [Ignore("not ready")]
    public static bool DeserializeSuccess()
    {
        Result okResult = Result.Success();
        var serialized = Serialize(okResult);

        Result result = Deserialize<Result>(serialized);

        return result.IsSuccess && !result.IsFailure;
    }

    private class Error
    {
    }

    public static async Task<bool> TimeSpanEnsure()
    {
        Task<Result<TimeSpan, Error>> sut = Task.FromResult(Result.Failure<TimeSpan, Error>(new Error()));

        Result<TimeSpan, Error> result = await sut.Ensure(t => true, new Error());

        return sut.Result.Equals(result);
    }

    [TestSvm(100)]
    public static bool TimeSpanEnsureTest()
    {
        return TimeSpanEnsure().Result;
    }

    [TestSvm(100)]
    public static long SimpleStopwatchTest()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        var a = new bool();
        a = true;
        stopwatch.Stop();

        return stopwatch.ElapsedTicks;
    }

    [TestSvm(100)]
    public static long StopwatchWithSleepTest()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        Thread.Sleep(5000);
        stopwatch.Stop();

        return stopwatch.ElapsedTicks;
    }

    [TestSvm(100)]
    public static bool ResultEnsure()
    {
        Result sut = Result.Success();
        Result result = sut.Ensure(() => false, "predicate failed");
        return !result.Equals(sut) && result.IsFailure && result.Error == "predicate failed";
    }

    [TestSvm(100)]
    public static bool ResultEnsure2(int n)
    {
        Result sut = Result.Success();
        Result result = sut.Ensure(() => n == 10, "predicate failed");
        return !result.Equals(sut) && result.IsFailure && result.Error == "predicate failed";
    }

    [TestSvm(100)]
    public static bool OnFailure(int n)
    {
        var expectedValue = new MyClass();

        var myResult = Result.Failure<MyClass>("abc");
        var newResult = myResult.OnFailureCompensate(error => Result.Success(expectedValue));

        return newResult.IsSuccess && newResult.Value == expectedValue;
    }

    [TestSvm(100)]
    public static bool OnFailure2(int n)
    {
        var expectedValue = new MyClass();
        expectedValue.x = n;

        var myResult = Result.Failure<MyClass>("abc");
        var newResult = myResult.OnFailureCompensate(error => Result.Success(expectedValue));

        return newResult.IsSuccess && newResult.Value == expectedValue;
    }

    [TestSvm(100)]
    public static bool Serialization()
    {
        Result okResult = Result.Success();
        ISerializable serializableObject = okResult;

        var serializationInfo = new SerializationInfo(typeof(Result), new FormatterConverter());
        serializableObject.GetObjectData(serializationInfo, new StreamingContext());

        return serializationInfo.GetBoolean(nameof(Result.IsSuccess)) &&
               !serializationInfo.GetBoolean(nameof(Result.IsFailure));
    }

    private class TestObject
    {
        public string String { get; set; }
        public int Number { get; set; }
    }

    [TestSvm(100)]
    public static bool Serialization2(int n)
    {
        TestObject language = new TestObject { Number = n, String = "C#" };
        Result<TestObject> okResult = Result.Success(language);
        ISerializable serializableObject = okResult;

        var serializationInfo = new SerializationInfo(typeof(Result), new FormatterConverter());
        serializableObject.GetObjectData(serializationInfo, new StreamingContext());
        return serializationInfo.GetValue(nameof(Result<TestObject>.Value), typeof(TestObject)) == language;
    }

    [TestSvm(100)]
    public static bool Serialization3(int n)
    {
        String s = "good" + n;
        TestObject language = new TestObject { Number = n, String = s };
        Result<TestObject> okResult = Result.Success(language);
        ISerializable serializableObject = okResult;

        var serializationInfo = new SerializationInfo(typeof(Result), new FormatterConverter());
        serializableObject.GetObjectData(serializationInfo, new StreamingContext());
        return serializationInfo.GetValue(nameof(Result<TestObject>.Value), typeof(TestObject)) == language;
    }

    enum ErrorType
    {
        Error1
    }

    [TestSvm(100)]
    public static bool ToStringTest()
    {
        Result<string, ErrorType> subject = Result.Failure<String, ErrorType>(ErrorType.Error1);
        return "Failure(Error1)" == subject.ToString();
    }

    private static bool FuncExecuted;
    public static T Func_T(int n)
    {
        FuncExecuted = n == 10;
        return T.Value;
    }

    [TestSvm(100)]
    public static bool ResultTry()
    {
        var result = Result.Try(() => Func_T(10));

        return result.IsSuccess && result.Value == T.Value && FuncExecuted;
    }

    [TestSvm(100)]
    public static bool ResultTry1(int n)
    {
        var result = Result.Try(() => Func_T(n));

        return result.IsSuccess && result.Value == T.Value && (FuncExecuted || n != 10);
    }

}
