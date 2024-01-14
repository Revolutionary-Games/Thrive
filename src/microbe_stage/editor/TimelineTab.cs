using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public class TimelineTab : PanelContainer
{
    [Export]
    public NodePath? GlobalEventsContainerPath;

    [Export]
    public NodePath LocalEventsContainerPath = null!;

    [Export]
    public NodePath ScrollContainerPath = null!;

    [Export]
    public NodePath LocalFilterButtonPath = null!;

    [Export]
    public NodePath GlobalFilterButtonPath = null!;

#pragma warning disable CA2213
    private readonly PackedScene customRichTextLabelScene;

    private readonly StyleBoxTexture eventHighlightStyleBox;

    private VBoxContainer globalEventsContainer = null!;
    private VBoxContainer localEventsContainer = null!;
    private ScrollContainer scrollContainer = null!;
    private Button localFilterButton = null!;
    private Button globalFilterButton = null!;
#pragma warning restore CA2213

    private double lastUpdateGameTime = -1;

    private Filters eventFilter = Filters.Local;

    private List<TimelineSection>? cachedLocalTimelineElements;
    private List<TimelineSection>? cachedGlobalTimelineElements;

    public TimelineTab()
    {
        customRichTextLabelScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");

        eventHighlightStyleBox = GD.Load<StyleBoxTexture>("res://src/microbe_stage/editor/TimelineEventHighlight.tres");
    }

    public enum Filters
    {
        /// <summary>
        ///   Means to show all events in the world.
        /// </summary>
        Global,

        /// <summary>
        ///   Means to only show events relevant to the patch the player is in.
        /// </summary>
        Local,
    }

    /// <summary>
    ///   Selects what events should be shown in the timeline tab.
    /// </summary>
    public Filters EventFilter
    {
        get => eventFilter;
        set
        {
            eventFilter = value;
            ApplyEventsFilter();
        }
    }

    public override void _Ready()
    {
        globalEventsContainer = GetNode<VBoxContainer>(GlobalEventsContainerPath);
        localEventsContainer = GetNode<VBoxContainer>(LocalEventsContainerPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);
        localFilterButton = GetNode<Button>(LocalFilterButtonPath);
        globalFilterButton = GetNode<Button>(GlobalFilterButtonPath);
    }

    public void UpdateTimeline(IEditorReportData editor, Patch? selectedPatch, Patch? patch = null)
    {
        if (editor.CurrentGame == null)
        {
            throw new ArgumentException(
                $"Editor must be initialized ({nameof(IEditorReportData.CurrentGame)} is null)");
        }

        // If global time changes, global events need to be updated
        if (Math.Abs(lastUpdateGameTime - editor.CurrentGame.GameWorld.TotalPassedTime) > MathUtils.EPSILON)
        {
            lastUpdateGameTime = editor.CurrentGame.GameWorld.TotalPassedTime;

            globalEventsContainer.FreeChildren();
            cachedGlobalTimelineElements = new List<TimelineSection>();

            foreach (var entry in editor.CurrentGame.GameWorld.EventsLog)
            {
                var section = new TimelineSection(customRichTextLabelScene, eventHighlightStyleBox,
                    (entry.Key, entry.Value));

                cachedGlobalTimelineElements.Add(section);
                globalEventsContainer.AddChild(section);
            }
        }

        localEventsContainer.FreeChildren();
        cachedLocalTimelineElements = new List<TimelineSection>();

        var targetPatch = patch ?? selectedPatch ?? editor.CurrentPatch;

        for (int i = targetPatch.History.Count - 1; i >= 0; i--)
        {
            var snapshot = targetPatch.History[i];
            var section = new TimelineSection(customRichTextLabelScene, eventHighlightStyleBox,
                (snapshot.TimePeriod, snapshot.EventsLog));

            cachedLocalTimelineElements.Add(section);
            localEventsContainer.AddChild(section);
        }

        ApplyEventsFilter();
    }

    public void TimelineAutoScrollToCurrentTimePeriod()
    {
        var scrollRect = scrollContainer.GetGlobalRect();
        var anchorRect = new Rect2(Vector2.Zero, Vector2.Zero);

        if (eventFilter == Filters.Global)
        {
            var last = cachedGlobalTimelineElements?.LastOrDefault();
            anchorRect = last?.HeaderGlobalRect ?? new Rect2(Vector2.Zero, Vector2.Zero);
        }
        else if (eventFilter == Filters.Local)
        {
            var last = cachedLocalTimelineElements?.LastOrDefault();
            anchorRect = last?.HeaderGlobalRect ?? new Rect2(Vector2.Zero, Vector2.Zero);
        }

        var diff = Mathf.Max(Mathf.Min(anchorRect.Position.y, scrollRect.Position.y), anchorRect.Position.y +
            anchorRect.Size.y - scrollRect.Size.y + (scrollRect.Size.y - anchorRect.Size.y));

        scrollContainer.ScrollVertical += (int)(diff - scrollRect.Position.y);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (GlobalEventsContainerPath != null)
            {
                GlobalEventsContainerPath.Dispose();
                LocalEventsContainerPath.Dispose();
                ScrollContainerPath.Dispose();
                LocalFilterButtonPath.Dispose();
                GlobalFilterButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ApplyEventsFilter()
    {
        switch (EventFilter)
        {
            case Filters.Global:
                localEventsContainer.Hide();
                globalEventsContainer.Show();
                globalFilterButton.Pressed = true;
                break;
            case Filters.Local:
                globalEventsContainer.Hide();
                localEventsContainer.Show();
                localFilterButton.Pressed = true;
                break;
            default:
                throw new Exception("Not a valid event filter");
        }

        Invoke.Instance.Queue(TimelineAutoScrollToCurrentTimePeriod);
    }

    private void OnFilterSelected(int index)
    {
        if (!Enum.IsDefined(typeof(Filters), index) || (Filters)index == EventFilter)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EventFilter = (Filters)index;
    }

    private class TimelineSection : VBoxContainer
    {
        private readonly PackedScene customRichTextLabelScene;
        private readonly StyleBoxTexture eventHighlightStyleBox;

        private readonly (double TimePeriod, List<GameEventDescription> Events) data;

#pragma warning disable CA2213
        private Control? headerContainer;
#pragma warning restore CA2213

        public TimelineSection(PackedScene customRichTextLabelScene, StyleBoxTexture eventHighlightStyleBox,
            (double TimePeriod, List<GameEventDescription> Events) data)
        {
            this.customRichTextLabelScene = customRichTextLabelScene;
            this.eventHighlightStyleBox = eventHighlightStyleBox;
            this.data = data;
        }

        public Rect2 HeaderGlobalRect => headerContainer?.GetGlobalRect() ?? throw new SceneTreeAttachRequired();

        public override void _Ready()
        {
            AddConstantOverride("separation", 2);

            headerContainer = new HBoxContainer();
            var spacer = new Control { RectMinSize = new Vector2(26, 0) };

            var timePeriodLabel = new Label
            {
                Text = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", data.TimePeriod) + " "
                    + TranslationServer.Translate("MEGA_YEARS"),
                RectMinSize = new Vector2(0, 55),
                Valign = Label.VAlign.Center,
            };

            timePeriodLabel.AddFontOverride("font", GetFont("jura_bold", "Fonts"));

            headerContainer.AddChild(spacer);
            headerContainer.AddChild(timePeriodLabel);
            AddChild(headerContainer);

            foreach (var entry in data.Events)
            {
                var itemContainer = new HBoxContainer();

                TextureRect? iconRect = null;
                if (!string.IsNullOrEmpty(entry.IconPath))
                {
                    iconRect = new TextureRect
                    {
                        RectMinSize = new Vector2(25, 25),
                        SizeFlagsVertical = (int)SizeFlags.ShrinkCenter,
                        Texture = GUICommon.LoadGuiTexture(entry.IconPath!),
                        Expand = true,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    };
                }

                var highlight = new PanelContainer
                {
                    SelfModulate = entry.Highlighted ? Colors.White : Colors.Transparent,
                    SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                };

                highlight.AddStyleboxOverride("panel", eventHighlightStyleBox);
                itemContainer.AddConstantOverride("separation", 5);

                var eventLabel = customRichTextLabelScene.Instance<CustomRichTextLabel>();
                eventLabel.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
                eventLabel.ExtendedBbcode = entry.Description.ToString();
                eventLabel.FitContentHeight = true;

                eventLabel.AddFontOverride("normal_font", GetFont("jura_almost_smaller", "Fonts"));
                eventLabel.AddFontOverride("bold_font", GetFont("jura_demibold_almost_smaller", "Fonts"));
                eventLabel.AddConstantOverride("line_separation", 0);

                if (iconRect != null)
                    itemContainer.AddChild(iconRect);
                itemContainer.AddChild(highlight);
                highlight.AddChild(eventLabel);
                AddChild(itemContainer);
            }

            if (data.Events?.Any() == false)
            {
                var noneLabelContainer = new HBoxContainer();
                var noneLabelSpacer = new Control { RectMinSize = new Vector2(25, 25) };
                var noneLabel = new Label { Text = TranslationServer.Translate("NO_EVENTS_RECORDED") };

                noneLabelContainer.AddConstantOverride("separation", 5);
                noneLabel.AddFontOverride("font", GetFont("jura_almost_smaller", "Fonts"));

                noneLabelContainer.AddChild(noneLabelSpacer);
                noneLabelContainer.AddChild(noneLabel);

                AddChild(noneLabelContainer);
            }
        }
    }
}
