// Thrive customization. Disable all warnings from this third party code
#pragma warning disable

namespace gdUnit4.addons.gdUnit4.src.dotnet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using GdUnit4.Api;
using GdUnit4.Core.Discovery;

using Godot;
using Godot.Collections;

// GdUnit4 GDScript - C# API wrapper
// ReSharper disable once CheckNamespace
public partial class GdUnit4CSharpApi : RefCounted
{
    [Signal]
    public delegate void ExecutionCompletedEventHandler();

    private static readonly object LockObject = new();

    private static Type? apiType;
    private static Assembly? gdUnit4Api;
    private CancellationTokenSource? executionCts;

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete)
            return;
        executionCts?.Dispose();
        executionCts = null;
    }

    private static Assembly GetApi()
    {
        if (gdUnit4Api != null)
            return gdUnit4Api;
        lock (LockObject)
            return gdUnit4Api ??= Assembly.Load("gdUnit4Api");
    }

    private static Type GetApiType()
    {
        if (apiType != null)
            return apiType;
        apiType = GetApi().GetType("GdUnit4.GdUnit4NetApiGodotBridge");
        return apiType!;
    }

    private static Version GdUnit4NetVersion()
        => GetApi().GetName().Version!;

    private static T InvokeApiMethod<T>(string methodName, params object[] args)
    {
        var method = GetApiType().GetMethod(methodName) ??
                     throw new MethodAccessException($"Can't invoke method {methodName}");
        return (T)method.Invoke(null, args)!;
    }

    public static bool FindGdUnit4NetAssembly()
    {
        try
        {
            return GetApi().GetType("GdUnit4.GdUnit4NetApiGodotBridge") != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string Version()
        => GdUnit4NetVersion().ToString();

    public static bool IsTestSuite(CSharpScript script)
        => InvokeApiMethod<bool>("IsTestSuite", script);

    public static Array<Dictionary> DiscoverTests(CSharpScript sourceScript)
    {
        try
        {
            // Get the list of test case descriptors from the API
            var testCaseDescriptors = InvokeApiMethod<List<TestCaseDescriptor>>("DiscoverTestsFromScript", sourceScript);
            // Convert each TestCaseDescriptor to a Dictionary
            return testCaseDescriptors
                .Select(descriptor => new Dictionary
                {
                    ["guid"] = descriptor.Id.ToString(),
                    ["managed_type"] = descriptor.ManagedType,
                    ["test_name"] = descriptor.ManagedMethod,
                    ["source_file"] = sourceScript.ResourcePath,
                    ["line_number"] = descriptor.LineNumber,
                    ["attribute_index"] = descriptor.AttributeIndex,
                    ["require_godot_runtime"] = descriptor.RequireRunningGodotEngine,
                    ["code_file_path"] = descriptor.CodeFilePath ?? "",
                    ["simple_name"] = descriptor.SimpleName,
                    ["fully_qualified_name"] = descriptor.FullyQualifiedName,
                    ["assembly_location"] = descriptor.AssemblyPath
                })
                .Aggregate(new Array<Dictionary>(), (array, dict) =>
                {
                    array.Add(dict);
                    return array;
                });
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error discovering tests: {e.Message}\n{e.StackTrace}");
            return new Array<Dictionary>();
        }
    }

    public void ExecuteAsync(Array<Dictionary> tests, Callable listener)
    {
        try
        {
            // Cancel any ongoing execution
            executionCts?.Cancel();
            executionCts?.Dispose();

            // Create new cancellation token source
            executionCts = new CancellationTokenSource();

            var testSuiteNodes = new List<TestSuiteNode> { BuildTestSuiteNodeFrom(tests) };
            InvokeApiMethod<Task>("ExecuteAsync", testSuiteNodes, listener, executionCts.Token)
                .GetAwaiter()
                .OnCompleted(() => EmitSignal(SignalName.ExecutionCompleted));
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error executing tests: {e.Message}\n{e.StackTrace}");
            Task.Run(() => { }).GetAwaiter().OnCompleted(() => EmitSignal(SignalName.ExecutionCompleted));
        }
    }

    public void CancelExecution()
    {
        try
        {
            executionCts?.Cancel();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error cancelling execution: {e.Message}");
        }
    }

    public static Dictionary CreateTestSuite(string sourcePath, int lineNumber, string testSuitePath)
        => InvokeApiMethod<Dictionary>("CreateTestSuite", sourcePath, lineNumber, testSuitePath);


    // Convert a set of Tests stored as Dictionaries to TestSuiteNode
    // all tests are assigned to a single test suit
    internal static TestSuiteNode BuildTestSuiteNodeFrom(Array<Dictionary> tests)
    {
        if (tests.Count == 0)
            throw new InvalidOperationException("Cant build 'TestSuiteNode' from an empty test set.");

        // Create a suite ID
        var suiteId = Guid.NewGuid();
        var firstTest = tests[0];
        var managedType = firstTest["managed_type"].AsString();
        var assemblyLocation = firstTest["assembly_location"].AsString();
        var sourceFile = firstTest["source_file"].AsString();

        // Create TestCaseNodes for each test in the suite
        var testCaseNodes = tests
            .Select(test => new TestCaseNode
                {
                    Id = Guid.Parse(test["guid"].AsString()),
                    ParentId = suiteId,
                    ManagedMethod = test["test_name"].AsString(),
                    LineNumber = test["line_number"].AsInt32(),
                    AttributeIndex = test["attribute_index"].AsInt32(),
                    RequireRunningGodotEngine = test["require_godot_runtime"].AsBool()
                }
            )
            .ToList();

        return new TestSuiteNode
        {
            Id = suiteId,
            ParentId = Guid.Empty,
            ManagedType = managedType,
            AssemblyPath = assemblyLocation,
            SourceFile = sourceFile,
            Tests = testCaseNodes
        };
    }
}
