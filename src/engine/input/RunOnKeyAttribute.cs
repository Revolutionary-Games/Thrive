using System;
using System.Collections.Generic;
using System.Reflection;

public class RunOnKeyAttribute : RunOnInputAttribute
{
    private IInputReceiver inputReceiver;
    public RunOnKeyAttribute(string inputString, InputType type)
    {
        InputString = inputString;
        Type = type;
    }


    public enum InputType
    {
        /// <summary>
        ///   Fires the method once when the key is pressed down
        /// </summary>
        Press,
        /// <summary>
        ///   Fires the method repeatedly when the key is held down
        /// </summary>
        Hold,
        /// <summary>
        ///   Fires the method once when the key is released
        /// </summary>
        Released,
        /// <summary>
        ///   Fires the method repeatedly and toggles when the key is pressed down
        /// </summary>
        ToggleHold,
    }

    public override IInputReceiver InputReceiver
    {
        get
        {
            return inputReceiver ??= Type switch
            {
                InputType.Hold => new InputBool(InputString),
                InputType.Press => new InputTrigger(InputString),
                InputType.Released => new InputReleaseTrigger(InputString),
                InputType.ToggleHold => new InputHoldToggle(InputString),
                _ => throw new NotSupportedException("Input type not supported"),
            };
        }
    }

    private string InputString { get; }
    private InputType Type { get; }
}
