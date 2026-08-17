using System;

/// <summary>
///   Like <see cref="CommandAttribute"/> but executing this command multicasts the invocation to all the instances of
///   the underlying object.
/// </summary>
/// <remarks>
///   <para>
///     This command attribute should be used carefully, because if it is invoked on too many instances by modifying the
///     <see cref="MaxAllowedRegisteredInstances"/> limit, a command execution might
///     freeze or crash the game. Please do not use this if the underlying object instances are expected to be temporary
///     or if they come in a huge number of registered instances.
///   </para>
///   <para>
///     Please note that multicast commands cannot be overloaded over static commands to prevent conflicts.
///   </para>
/// </remarks>
/// <param name="maxAllowedRegisteredInstances">
///   The maximum number of allowed registered instances for the execution of this command. It is recommended this is
///   kept as low as possible to prevent too many invocations.
/// </param>
/// <param name="failOnTooManyInstances">
///   If this is true and the number of registered instances is greater than
///   <see cref="MaxAllowedRegisteredInstances"/>, then the command registry will refuse routing the command to any
///   instance. If this is false, the command will only be routed to the first MaxAllowedRegisteredInstances registered.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class MulticastCommandAttribute(string commandName, bool isCheat, string helpText = "",
    int maxAllowedRegisteredInstances = 32, bool failOnTooManyInstances = true) :
    CommandAttribute(commandName, isCheat, helpText)
{
    public readonly int MaxAllowedRegisteredInstances = maxAllowedRegisteredInstances;
    public readonly bool FailOnTooManyInstances = failOnTooManyInstances;
}
