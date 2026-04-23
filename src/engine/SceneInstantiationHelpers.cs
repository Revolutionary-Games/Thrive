using System;
using Godot;

/// <summary>
///   Helpers for instantiating Godot scenes with a retry guard against rare engine scene instancing failures.
/// </summary>
public static class SceneInstantiationHelpers
{
    public const int DefaultInstantiateAttempts = 3;

    public static T? RetryCreate<T>(Func<T?> createInstance, string description,
        Func<T, string?>? validate = null, Action<T>? discardInvalid = null, int attempts = DefaultInstantiateAttempts,
        Action<string>? reportFailure = null)
        where T : class
    {
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(attempts), "Attempt count must be at least 1");

        reportFailure ??= GD.PrintErr;

        for (int attempt = 1; attempt <= attempts; ++attempt)
        {
            T? instance;

            try
            {
                instance = createInstance();
            }
            catch (Exception e)
            {
                reportFailure($"Failed to create {description} on attempt {attempt}/{attempts}: {e}");
                continue;
            }

            if (instance == null)
            {
                reportFailure($"Failed to create {description} on attempt {attempt}/{attempts}: returned null");
                continue;
            }

            var validationFailure = validate?.Invoke(instance);

            if (validationFailure == null)
                return instance;

            reportFailure(
                $"Created invalid {description} on attempt {attempt}/{attempts}: {validationFailure}");
            discardInvalid?.Invoke(instance);
        }

        reportFailure($"Giving up on creating {description} after {attempts} failed attempts");
        return null;
    }

    public static T? InstantiateSceneWithRetries<T>(string scenePath, Func<T, string?>? validate = null,
        int attempts = DefaultInstantiateAttempts)
        where T : Node
    {
        return RetryCreate(() =>
            {
                var scene = GD.Load<PackedScene>(scenePath);
                return scene.Instantiate<T>();
            },
            scenePath, validate, node => node.Free(), attempts);
    }
}
