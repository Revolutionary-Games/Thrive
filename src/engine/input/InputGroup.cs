using System.Collections.Generic;
using Godot;

/// <summary>
///   Groups a set of IInputReceiver objects to act on them more easily
/// </summary>
public class InputGroup : IInputReceiver
{
    private readonly List<IInputReceiver> inputs;

    private bool justLostFocus;

    public InputGroup(List<IInputReceiver> inputs)
    {
        this.inputs = inputs;
    }

    /// <summary>
    ///   Handles input passing to all inputs in this group.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this doesn't call `GetTree().SetInputAsHandled();` automatically.
    ///   </para>
    /// </remarks>
    public bool CheckInput(InputEvent inputEvent)
    {
        // For some reason while losing focus we still get key held down events after the focus loss, so we ignore
        // them for a frame
        if (justLostFocus)
        {
            return false;
        }

        foreach (var input in inputs)
        {
            if (input.CheckInput(inputEvent))
            {
                return true;
            }
        }

        return false;
    }

    public void FocusLost()
    {
        foreach (var input in inputs)
            input.FocusLost();

        justLostFocus = true;
    }

    /// <summary>
    ///   Used to detect when frame changes to make focus loss detection work
    /// </summary>
    public void OnFrameChanged()
    {
        if (justLostFocus)
            justLostFocus = false;
    }
}
