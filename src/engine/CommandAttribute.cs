using System;

/// <summary>
///   An attribute flagging the following method that it should be registered as an invokable by a DebugConsole Command. <br/>
///   The Console Command will be registered by the CommandRegistry Singleton at startup, and the method will be invoked
///   by reflection when the user submits the corresponding command. <br/>
///   If the submitted command is malformed or invalid, the method won't be invoked and the CommandRegistry will log the
///   problem to the Console. <br/>
///   Please note that this attribute is only valid on static methods. The CommandRegistry will generate a runtime
///   warning and simply discard the command on non-static methods.
/// </summary>
/// <param name="commandName">The command name. This is the keyword the user submits in the console to invoke the method.</param>
/// <param name="isCheat">Whether this command should report cheats used to the game.</param>
/// <param name="helpText">A synthetic optional help text string. This string will be shown when the "help" command is executed in the console.</param>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string commandName, bool isCheat, string helpText = "") : Attribute
{
    public string CommandName { get; } = commandName;
    public string HelpText { get; } = helpText;
    public bool IsCheat { get; } = isCheat;
}
