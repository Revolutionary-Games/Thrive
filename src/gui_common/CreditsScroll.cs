using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public class CreditsScroll : Container
{
    private const float GameNameEndOffset = 370;
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
        int offset = 10 + (int)smoothOffset;

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

        var builder = new StringBuilder(500);

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

            // Combine the names to a single list for probably more efficiency
            // If past team leads need to be marked might need to use rich text label or some other approach there

            bool first = true;

            foreach (var member in pair.Value)
            {
                if (!first)
                    builder.Append('\n');

                first = false;
                builder.Append(member.Name);
            }

            var memberLabel = new DynamicPart(offset)
            {
                Text = builder.ToString(),
                RectMinSize = new Vector2(RectSize.x, 0),
                Align = Label.AlignEnum.Center,
            };
            AddDynamicItem(memberLabel);
            builder.Clear();

            offset += (int)memberLabel.RectSize.y + ExtraOffsetAfterTeam;
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
        part.UpdatePosition(smoothOffset, RectSize.y);
    }

    private void UpdateDynamicItems()
    {
        var height = RectSize.y;

        foreach (var part in dynamicParts)
        {
            part.UpdatePosition(smoothOffset, height);

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

        public bool HasBeenVisible { get; set; }

        public Action OnBecomeVisible { get; set; }

        public Action OnEnded { get; set; }

        public void UpdatePosition(float currentScroll, float containerHeight)
        {
            RectPosition = new Vector2(RectPosition.x, containerHeight + startPosition - currentScroll);
        }
    }
}
