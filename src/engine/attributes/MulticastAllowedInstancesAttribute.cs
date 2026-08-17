using System;

/// <summary>
///   This attribute is to be used in classes that have <see cref="MulticastCommandAttribute"/> methods.
///   It specifies the maximum number of allowed registered instances for the execution of this command.
/// </summary>
/// <param name="maxAllowedRegisteredInstances">
///   The maximum number of allowed registered instances for the execution of this command. It is recommended this is
///   kept as low as possible to prevent too many invocations.
/// </param>
/// <param name="failOnTooManyInstances">
///   If this is true and the number of registered instances is greater than
///   <see cref="MaxAllowedRegisteredInstances"/>, then the command registry will refuse routing the command to any
///   instance. If this is false, the command will only be routed to the first MaxAllowedRegisteredInstances registered.
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class MulticastAllowedInstancesAttribute(int maxAllowedRegisteredInstances = 32,
    bool failOnTooManyInstances = true) : Attribute
{
    public static readonly MulticastAllowedInstancesAttribute Default = new();

    public readonly int MaxAllowedRegisteredInstances = maxAllowedRegisteredInstances;
    public readonly bool FailOnTooManyInstances = failOnTooManyInstances;
}
