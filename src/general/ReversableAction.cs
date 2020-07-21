﻿using System;

/// <summary>
///   Action that can be undone and redone
/// </summary>
public abstract class ReversableAction
{
    /// <summary>
    ///   True when the action has been performed and can be undone
    /// </summary>
    public bool Performed { get; private set; }

    /// <summary>
    ///   Does this action
    /// </summary>
    public void Perform()
    {
        if (Performed)
            throw new InvalidOperationException("cannot perform already performed action");

        DoAction();
        Performed = true;
    }

    /// <summary>
    ///   Undoes this action
    /// </summary>
    public void Undo()
    {
        if (!Performed)
            throw new InvalidOperationException("cannot undo not performed action");

        UndoAction();
        Performed = false;
    }

    // Subclass callbacks
    public abstract void DoAction();
    public abstract void UndoAction();
}
