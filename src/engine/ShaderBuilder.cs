using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Assembles shader source out of reusable module files, so that code shared between the compute shaders of the
///   Forward+ renderer and the gdshaders of the Compatibility renderer only exists once.
/// </summary>
/// <remarks>
///   <para>
///     Modules are plain <c>.gdshaderinc</c> files. Modules shared across backends must be written in the subset of
///     GLSL that Godot's shading language also accepts, and must not declare uniforms of their own.
///   </para>
///   <para>
///     Backend-specific modules, such as a backend's interface module, are exempt from this. They supply the
///     declarations of their backend (version directive, layouts, uniforms, push constants) and define the macros the
///     shared modules read their parameters through.
///   </para>
/// </remarks>
public sealed class ShaderBuilder
{
    private readonly Dictionary<string, ShaderModule> modules = new();
    private readonly HashSet<string> emitted = new();
    private readonly List<string> pending = new();
    private readonly StringBuilder result = new();

    public void AddModule(string name, string path, params string[] dependencies)
    {
        modules[name] = new ShaderModule(path, dependencies);
    }

    /// <summary>
    ///   Builds the source for the given root modules.
    /// </summary>
    public string Build(params string[] roots)
    {
        result.Clear();
        emitted.Clear();
        pending.Clear();

        foreach (var root in roots)
            Emit(root);

        return result.ToString();
    }

    private static string LoadModuleCode(string path)
    {
        var cacheMode = FeatureInformation.IsExported() ?
            ResourceLoader.CacheMode.Reuse :
            ResourceLoader.CacheMode.Ignore;

        var include = ResourceLoader.Load<ShaderInclude>(path, cacheMode: cacheMode);

        if (include is null)
            throw new InvalidOperationException($"Failed to load shader module file: {path}");

        return include.Code;
    }

    private void Emit(string name)
    {
        if (emitted.Contains(name))
            return;

        if (!modules.TryGetValue(name, out var module))
            throw new InvalidOperationException($"Shader module '{name}' has not been registered");

        if (pending.Contains(name))
        {
            throw new InvalidOperationException("Cyclic shader module dependency: " +
                $"{string.Join(" -> ", pending)} -> {name}");
        }

        pending.Add(name);

        foreach (var dependency in module.Dependencies)
            Emit(dependency);

        pending.RemoveAt(pending.Count - 1);
        emitted.Add(name);

        result.Append("// module: ").Append(name).Append('\n');
        result.Append(LoadModuleCode(module.Path)).Append('\n');
    }

    private sealed class ShaderModule(string path, string[] dependencies)
    {
        public readonly string Path = path;
        public readonly string[] Dependencies = dependencies;
    }
}
