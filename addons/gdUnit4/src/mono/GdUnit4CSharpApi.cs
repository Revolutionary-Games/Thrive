// Thrive customizations to disable warnings in this imported file
#pragma warning disable SA1507,SA1513,SA1027,SA1210,SA1202

// End of changes

using System;
using System.Reflection;
using System.Linq;

using Godot;
using Godot.Collections;
using GdUnit4;


// GdUnit4 GDScript - C# API wrapper
public partial class GdUnit4CSharpApi : Godot.GodotObject
{
	private static Type? apiType;

	private static Type GetApiType()
	{
		if (apiType == null)
		{
			var assembly = Assembly.Load("gdUnit4Api");
			apiType = GdUnit4NetVersion() < new Version(4, 2, 2) ?
				assembly.GetType("GdUnit4.GdUnit4MonoAPI") :
				assembly.GetType("GdUnit4.GdUnit4NetAPI");
			Godot.GD.PrintS($"GdUnit4CSharpApi type:{apiType} loaded.");
		}
		return apiType!;
	}

	private static Version GdUnit4NetVersion()
	{
		var assembly = Assembly.Load("gdUnit4Api");
		return assembly.GetName().Version!;
	}

	private static T InvokeApiMethod<T>(string methodName, params object[] args)
	{
		var method = GetApiType().GetMethod(methodName)!;
		return (T)method.Invoke(null, args)!;
	}

	public static string Version() => GdUnit4NetVersion().ToString();

	public static bool IsTestSuite(string classPath) => InvokeApiMethod<bool>("IsTestSuite", classPath);

	public static RefCounted Executor(Node listener) => InvokeApiMethod<RefCounted>("Executor", listener);

	public static CsNode? ParseTestSuite(string classPath) => InvokeApiMethod<CsNode?>("ParseTestSuite", classPath);

	public static Dictionary CreateTestSuite(string sourcePath, int lineNumber, string testSuitePath) =>
		InvokeApiMethod<Dictionary>("CreateTestSuite", sourcePath, lineNumber, testSuitePath);
}
