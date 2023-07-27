using Godot;

/// <summary>
///   Extends <see cref="CreatureStageHUDBase{TStage}"/> with multiplayer-specific parts, like scoreboard, chatbox, etc.
/// </summary>
public abstract class MultiplayerStageHUDBase<TStage> : CreatureStageHUDBase<TStage>
    where TStage : Object, ICreatureStage
{
    [Export]
    public NodePath ChatBoxPath = null!;

    [Export]
    public NodePath InfoScreenPath = null!;

    [Export]
    public NodePath ScoreBoardPath = null!;

    [Export]
    public NodePath WelcomeDialogPath = null!;

    protected AnimationPlayer chatBoxAnimationPlayer = null!;

    protected ChatBox chatBox = null!;
    protected Control infoScreen = null!;
    protected NetworkPlayerList scoreBoard = null!;
    protected TutorialDialog welcomeDialog = null!;

    // The values of the following variable is the opposite of the expected value.
    // I.e. its value is true when its respective panel is collapsed.
    private bool chatBoxActive = true;

    public override void _Ready()
    {
        base._Ready();

        chatBoxAnimationPlayer = GetNode<AnimationPlayer>(ChatBoxAnimationPlayerPath);
        chatBox = GetNode<ChatBox>(ChatBoxPath);
        infoScreen = GetNode<Control>(InfoScreenPath);
        scoreBoard = GetNode<NetworkPlayerList>(ScoreBoardPath);
        welcomeDialog = GetNode<TutorialDialog>(WelcomeDialogPath);
    }

    public override void Init(TStage containedInStage)
    {
        base.Init(containedInStage);

        ChatButtonPressed(true);
    }

    public virtual void ToggleInfoScreen()
    {
        infoScreen.Visible = !infoScreen.Visible;
    }

    public void SortScoreBoard()
    {
        scoreBoard.SortHighestScoreFirst();
    }

    public NetworkPlayerLog GetFirstOnTheScoreBoard()
    {
        return scoreBoard.GetFirst();
    }

    public NetworkPlayerLog? GetPlayerLog(int peerId)
    {
        return scoreBoard.GetPlayer(peerId);
    }

    public void OnChatFocused()
    {
        bottomLeftBar.ChatPressed = true;
        ChatButtonPressed(true);
    }

    protected override void OnFinishTransitioning()
    {
        base.OnFinishTransitioning();

        if (!Settings.Instance.IsNoticePermanentlyDismissed(DismissibleNotice.MultiplayerWelcome))
            welcomeDialog.PopupCenteredShrink();
    }

    private void ChatButtonPressed(bool wantedState)
    {
        if (chatBoxActive == !wantedState)
            return;

        if (!chatBoxActive)
        {
            chatBoxActive = true;
            chatBoxAnimationPlayer.Play("Close");
        }
        else
        {
            chatBoxActive = false;
            chatBoxAnimationPlayer.Play("Open");
        }
    }

    private void OnWelcomeDialogClosed()
    {
        Settings.Instance.PermanentlyDismissNotice(DismissibleNotice.MultiplayerWelcome);
    }
}
