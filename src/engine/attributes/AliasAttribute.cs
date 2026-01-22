using System;

/// <summary>
///   An attribute used by enum fields to define aliases for command arguments.
/// </summary>
/// <param name="aliases">The other names the enum field may be referred as, in a command parameter.</param>
[AttributeUsage(AttributeTargets.Field)]
public class AliasAttribute(params string[] aliases) : Attribute
{
    public string[] Aliases { get; } = aliases;
}
