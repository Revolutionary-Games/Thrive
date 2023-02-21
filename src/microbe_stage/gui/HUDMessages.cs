using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows message lines on screen that fade after some time to give the player some gameplay related messages
/// </summary>
public class HUDMessages : VBoxContainer
{
    private readonly List<(IHUDMessage Message, Label Displayer)> hudMessages = new();

    private string multipliedMessageTemplate = string.Empty;

    public override void _Ready()
    {
        multipliedMessageTemplate = TranslationServer.Translate("HUD_MESSAGE_MULTIPLE");
    }

    public override void _Process(float delta)
    {
        bool clean = false;

        foreach (var (message, displayer) in hudMessages)
        {
            message.TimeRemaining -= delta;

            if (message.TimeRemaining < 0)
            {
                displayer.QueueFree();
                clean = true;
                continue;
            }

            // TODO: update fade, first half should fade slower, and then again fade fast
        }

        if (clean)
            hudMessages.RemoveAll(t => t.Message.TimeRemaining < 0);
    }

    public void ShowMessage(IHUDMessage message)
    {
        // First combine to an existing message if possible
        foreach (var (existingMessage, existingLabel) in hudMessages)
        {
            if (existingMessage.IsSameMessage(message))
            {
                existingMessage.UpdateFromOtherMessage(message);
                existingLabel.Text = TextForMessage(existingMessage);

                // Reset the fade time for the message that appeared again
                existingMessage.TimeRemaining = existingMessage.OriginalTimeRemaining;

                // TODO: should we have an absolute time cutoff that forces a message out if it's been visible for
                // more than a minute?

                return;
            }
        }

        // Can't combine, need to add a new one
        var label = new Label();

        // TODO: allow export param to set the font

        label.SizeFlagsHorizontal = (int)SizeFlags.Expand;
        label.Align = Label.AlignEnum.Center;
        label.Autowrap = true;
        label.MouseFilter = MouseFilterEnum.Ignore;

        label.Text = TextForMessage(message);
        AddChild(label);

        hudMessages.Add((message, label));
    }

    public void ShowMessage(string simpleMessage, DisplayDuration duration = DisplayDuration.Normal)
    {
        ShowMessage(new SimpleHUDMessage(simpleMessage, duration));
    }

    private static float TimeToFadeFromDuration(DisplayDuration duration)
    {
        return duration switch
        {
            DisplayDuration.Short => 2,
            DisplayDuration.Normal => 4,
            DisplayDuration.Long => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null),
        };
    }

    private string TextForMessage(IHUDMessage message)
    {
        if (message.Multiplier < 2)
            return message.ToString();

        return multipliedMessageTemplate.FormatSafe(message.ToString(), message.Multiplier);
    }
}
