using Godot;

public class KickPlayerDialog : Control
{
    [Export]
    public NodePath ReasonEditPath = null!;

    private CustomConfirmationDialog dialog = null!;
    private LineEdit reasonEdit = null!;

    private int id;

    public override void _Ready()
    {
        reasonEdit = GetNode<LineEdit>(ReasonEditPath);

        dialog = GetNode<CustomConfirmationDialog>("CustomConfirmationDialog");
    }

    public void RequestKick(int peerId)
    {
        reasonEdit.Clear();
        dialog.PopupCenteredShrink();
        id = peerId;
    }

    private void OnKickConfirmed()
    {
        OnKickConfirmed(reasonEdit.Text);
    }

    private void OnKickConfirmed(string newText)
    {
        if (id == 1)
        {
            NetworkManager.Instance.Print("Attempting to kick host/server, this is not allowed");
            return;
        }

        NetworkManager.Instance.Kick(id, newText);

        dialog.Hide();
    }

    private void OnKickCancelled()
    {
        id = -1;
    }
}
