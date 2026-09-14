public interface IHUDMessageReceiver
{
    public void ShowMessage(IHUDMessage message);
}

public static class HUDMessageReceiverExtensions
{
    public static void ShowMessage(this IHUDMessageReceiver receiver, string simpleMessage,
        DisplayDuration duration = DisplayDuration.Normal)
    {
        receiver.ShowMessage(new SimpleHUDMessage(simpleMessage, duration));
    }
}
