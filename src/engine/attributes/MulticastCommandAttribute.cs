using System;

/// <summary>
///   Like <see cref="CommandAttribute"/> but executing this command multicasts the invocation to all the instances of
///   the underlying object.
/// </summary>
/// <remarks>
///   <para>
///     This command attribute should be used carefully, because if it is invoked on too many instances by modifying the
///     <see cref="MulticastAllowedInstancesAttribute.MaxAllowedRegisteredInstances"/> limit, a command execution might
///     freeze or crash the game. Please do not use this if the underlying object instances are expected to be temporary
///     or if they come in a huge number of registered instances.
///   </para>
///   <para>
///     Please note that multicast commands cannot be overloaded over static commands to prevent conflicts.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class MulticastCommandAttribute(string commandName, bool isCheat, string helpText = "") :
    CommandAttribute(commandName, isCheat, helpText);
