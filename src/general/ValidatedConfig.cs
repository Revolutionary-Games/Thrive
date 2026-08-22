using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Base for configuration resources that can check whether their own values make sense.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="Validate"/> always checks and reports every problem it finds. <see cref="ValidateOnce"/> only
///     rechecks when something actually changed since the last call, which makes it safe to call from per-frame code
///     that feeds shader parameters without flooding the log with the same message every frame.
///   </para>
///   <para>
///     Implementations capture exactly the values their <see cref="Validate"/> looks at, so a change to a value with
///     nothing to check about it doesn't trigger a pointless recheck.
///   </para>
/// </remarks>
public abstract partial class ValidatedConfig : Resource
{
    private float[]? previousValues;
    private bool previousResult = true;

    /// <summary>
    ///   How many values <see cref="CaptureValues"/> writes.
    /// </summary>
    protected abstract int ValueCount { get; }

    /// <summary>
    ///   Checks that every value is in range and consistent with the others, reporting all problems found.
    /// </summary>
    /// <returns>True when everything is valid.</returns>
    public abstract bool Validate();

    /// <summary>
    ///   Like <see cref="Validate"/>, but only rechecks when a value has changed since the last call. In between, the
    ///   result of the last real check is returned without printing anything again.
    /// </summary>
    /// <returns>True when everything is valid.</returns>
    public bool ValidateOnce()
    {
        int count = ValueCount;

        Span<float> current = stackalloc float[count];
        CaptureValues(current);

        var currentBits = MemoryMarshal.Cast<float, int>(current);

        if (previousValues is not null && previousValues.Length == count &&
            currentBits.SequenceEqual(MemoryMarshal.Cast<float, int>(previousValues.AsSpan())))
        {
            return previousResult;
        }

        if (previousValues is null || previousValues.Length != count)
            previousValues = new float[count];

        current.CopyTo(previousValues);

        previousResult = Validate();
        return previousResult;
    }

    /// <summary>
    ///   Writes the values <see cref="Validate"/> inspects into the given buffer, so that changes to them can be
    ///   detected.
    /// </summary>
    protected abstract void CaptureValues(Span<float> destination);

    /// <summary>
    ///   Reports a problem when the condition doesn't hold.
    /// </summary>
    /// <returns>The condition, so the caller can accumulate it.</returns>
    protected bool Check(bool condition, string message)
    {
        if (condition)
            return true;

        GD.PrintErr(GetType().Name, ": ", message);
        return false;
    }
}
