using System;
using System.Collections.Generic;
using Godot;

public class CreditsScroll : Container
{
    private const float GameNameEndOffset = 340;
    private const float GameNameInvisibleOffset = 1200;

    private const int OffsetAfterTeamName = 10;
    private const int OffsetBetweenNames = 5;
    private const int ExtraOffsetAfterTeam = 30;

    private readonly List<DynamicPart> destroyedDynamicParts = new List<DynamicPart>();

    private CreditsPhase phase = CreditsPhase.NotRunning;
    private List<DynamicPart> dynamicParts;

    private bool scrolling = true;
    private float scrollOffset;
    private float smoothOffset;

    private GameCredits credits;

    private Control logo;
    private Control revolutionaryGames;
    private Control supportedBy;
    private Control developersHeading;

    [Signal]
    public delegate void OnFinishedSignal();

    private enum CreditsPhase
    {
        NotRunning,
        GameName,
        DynamicPartEarly,
        DynamicPart,
    }

    [Export]
    public float ScrollSpeed { get; set; } = 40;

    [Export]
    public bool AutoStart { get; set; } = true;

    [Export]
    public NodePath LogoPath { get; set; }

    [Export]
    public NodePath RevolutionaryGamesPath { get; set; }

    [Export]
    public NodePath SupportedByPath { get; set; }

    [Export]
    public NodePath DevelopersHeadingPath { get; set; }

    public override void _Ready()
    {
        logo = GetNode<Control>(LogoPath);
        revolutionaryGames = GetNode<Control>(RevolutionaryGamesPath);
        supportedBy = GetNode<Control>(SupportedByPath);
        developersHeading = GetNode<Control>(DevelopersHeadingPath);

        credits = SimulationParameters.Instance.GetCredits();

        if (phase == CreditsPhase.NotRunning && AutoStart)
        {
            Setup();
        }
    }

    public override void _Process(float delta)
    {
        if (!scrolling || phase == CreditsPhase.NotRunning)
            return;

        scrollOffset += delta * ScrollSpeed;
        smoothOffset = Mathf.Round(scrollOffset);

        switch (phase)
        {
            case CreditsPhase.GameName:
            {
                UpdateStaticItemPositions();

                if (scrollOffset > GameNameEndOffset)
                {
                    phase = CreditsPhase.DynamicPartEarly;
                    LoadCurrentDevelopers();
                }

                break;
            }

            case CreditsPhase.DynamicPartEarly:
            {
                UpdateDynamicItems();
                UpdateStaticItemPositions();

                if (scrollOffset > GameNameInvisibleOffset)
                {
                    logo.Visible = false;
                    revolutionaryGames.Visible = false;
                    supportedBy.Visible = false;
                    developersHeading.Visible = false;

                    phase = CreditsPhase.DynamicPart;
                }

                break;
            }

            case CreditsPhase.DynamicPart:
            {
                UpdateDynamicItems();

                // End once all parts have scrolled off-screen
                if (dynamicParts.Count < 1)
                {
                    EmitSignal(nameof(OnFinishedSignal));
                    scrolling = false;
                }

                break;
            }

            default:
                throw new InvalidOperationException("invalid phase");
        }
    }

    public void Restart()
    {
        scrolling = true;

        if (scrollOffset == 0 && phase != CreditsPhase.NotRunning && phase == CreditsPhase.GameName)
            return;

        Setup();
    }

    public void Pause()
    {
        scrolling = false;
    }

    public void Resume()
    {
        scrolling = true;
    }

    private void Setup()
    {
        scrollOffset = 0;
        smoothOffset = 0;
        phase = CreditsPhase.GameName;

        logo.Visible = true;
        revolutionaryGames.Visible = true;
        supportedBy.Visible = true;
        developersHeading.Visible = true;

        UpdateStaticItemPositions();

        if (dynamicParts != null)
        {
            foreach (var part in dynamicParts)
            {
                part.DetachAndQueueFree();
            }
        }

        dynamicParts = new List<DynamicPart>();
    }

    private void LoadCurrentDevelopers()
    {
        int offset = (int)RectSize.y + 50 + (int)smoothOffset;

        // TODO: use different fonts for different parts
        var currentLabel = new DynamicPart(offset)
        {
            Text = TranslationServer.Translate("CURRENT_DEVELOPERS"),
            RectMinSize = new Vector2(RectSize.x, 0),
            Align = Label.AlignEnum.Center,
        };
        AddDynamicItem(currentLabel);

        offset += (int)currentLabel.RectSize.y + 50;

        // TODO: team leads

        // Team members
        foreach (var pair in credits.Developers.Current)
        {
            var teamNameLabel = new DynamicPart(offset)
            {
                // TODO: translate this dynamic data somehow
                Text = pair.Key,
                RectMinSize = new Vector2(RectSize.x, 0),
                Align = Label.AlignEnum.Center,
            };
            AddDynamicItem(teamNameLabel);

            offset += (int)teamNameLabel.RectSize.y + OffsetAfterTeamName;

            foreach (var member in pair.Value)
            {
                var memberLabel = new DynamicPart(offset)
                {
                    Text = member.Name,
                    RectMinSize = new Vector2(RectSize.x, 0),
                    Align = Label.AlignEnum.Center,
                };
                AddDynamicItem(memberLabel);

                offset += (int)memberLabel.RectSize.y + OffsetBetweenNames;
            }

            offset += ExtraOffsetAfterTeam;
        }
    }

    private void UpdateStaticItemPositions()
    {
        logo.RectPosition = new Vector2(0, RectSize.y - smoothOffset);
        revolutionaryGames.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 200);
        supportedBy.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 250);
        developersHeading.RectPosition = new Vector2(0, RectSize.y - smoothOffset + 300);
    }

    private void AddDynamicItem(DynamicPart part)
    {
        AddChild(part);
        dynamicParts.Add(part);
        part.CurrentScroll = smoothOffset;
    }

    private void UpdateDynamicItems()
    {
        var height = RectSize.y;

        foreach (var part in dynamicParts)
        {
            part.CurrentScroll = smoothOffset;

            if (!part.HasBeenVisible && part.RectPosition.y < height)
            {
                part.HasBeenVisible = true;
                part.OnBecomeVisible?.Invoke();
            }

            if (part.RectPosition.y + part.RectSize.y + 10 < 0)
            {
                destroyedDynamicParts.Add(part);
            }
        }

        foreach (var part in destroyedDynamicParts)
        {
            part.OnEnded?.Invoke();

            part.DetachAndQueueFree();
            dynamicParts.Remove(part);
        }

        destroyedDynamicParts.Clear();
    }

    private class DynamicPart : Label
    {
        private readonly float startPosition;

        public DynamicPart(float startPosition)
        {
            this.startPosition = startPosition;
        }

        public float CurrentScroll
        {
            set
            {
                RectPosition = new Vector2(RectPosition.x, startPosition - value);
            }
        }

        public bool HasBeenVisible { get; set; }

        public Action OnBecomeVisible { get; set; }

        public Action OnEnded { get; set; }
    }
}
