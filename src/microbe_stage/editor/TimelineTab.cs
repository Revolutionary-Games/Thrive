﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
///   Shows a timeline of world or local patch events on a long timescale (from auto-evo data)
/// </summary>
public partial class TimelineTab : PanelContainer
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
    [Export]
    public LabelSettings TimePeriodTitleFont = null!;

    [Export]
    public LabelSettings EventLabelNormalFont = null!;

    [Export]
    public LabelSettings EventLabelBoldFont = null!;

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

            foreach (var entry in editor.CurrentGame.GameWorld.EventsLog.OrderBy(s => s.Key))
            {
                var section = new TimelineSection(customRichTextLabelScene, eventHighlightStyleBox,
                    (entry.Key, entry.Value))
                {
                    TitleFont = TimePeriodTitleFont,
                    NormalFont = EventLabelNormalFont,
                    BoldFont = EventLabelBoldFont,
                };

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
                (snapshot.TimePeriod, snapshot.EventsLog))
            {
                TitleFont = TimePeriodTitleFont,
                NormalFont = EventLabelNormalFont,
                BoldFont = EventLabelBoldFont,
            };

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

        var diff = MathF.Max(MathF.Min(anchorRect.Position.Y, scrollRect.Position.Y), anchorRect.Position.Y +
            anchorRect.Size.Y - scrollRect.Size.Y + (scrollRect.Size.Y - anchorRect.Size.Y));

        scrollContainer.ScrollVertical += (int)(diff - scrollRect.Position.Y);
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
                globalFilterButton.ButtonPressed = true;
                break;
            case Filters.Local:
                globalEventsContainer.Hide();
                localEventsContainer.Show();
                localFilterButton.ButtonPressed = true;
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

    // Instances are created only through code
    // ReSharper disable once Godot.MissingParameterlessConstructor
    private partial class TimelineSection : VBoxContainer
    {
#pragma warning disable CA2213
        public LabelSettings TitleFont = null!;
        public LabelSettings NormalFont = null!;
        public LabelSettings BoldFont = null!;
#pragma warning restore CA2213

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
            AddThemeConstantOverride("separation", 2);

            headerContainer = new HBoxContainer();
            var spacer = new Control { CustomMinimumSize = new Vector2(26, 0) };

            var timePeriodLabel = new Label
            {
                Text = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", data.TimePeriod) + " "
                    + Localization.Translate("MEGA_YEARS"),
                CustomMinimumSize = new Vector2(0, 55),
                VerticalAlignment = VerticalAlignment.Center,
            };

            timePeriodLabel.LabelSettings = TitleFont;

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
                        CustomMinimumSize = new Vector2(25, 25),
                        SizeFlagsVertical = SizeFlags.ShrinkCenter,
                        Texture = GUICommon.LoadGuiTexture(entry.IconPath!),
                        ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    };
                }

                var highlight = new PanelContainer
                {
                    SelfModulate = entry.Highlighted ? Colors.White : Colors.Transparent,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };

                highlight.AddThemeStyleboxOverride("panel", eventHighlightStyleBox);
                itemContainer.AddThemeConstantOverride("separation", 5);

                var eventLabel = customRichTextLabelScene.Instantiate<CustomRichTextLabel>();
                eventLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                eventLabel.ExtendedBbcode = entry.Description.ToString();
                eventLabel.FitContent = true;

                eventLabel.AddThemeFontOverride("normal_font", NormalFont.Font);
                eventLabel.AddThemeFontSizeOverride("normal_font_size", NormalFont.FontSize);

                eventLabel.AddThemeFontOverride("bold_font", BoldFont.Font);
                eventLabel.AddThemeFontSizeOverride("bold_font_size", BoldFont.FontSize);

                eventLabel.AddThemeConstantOverride("line_separation", 0);

                if (iconRect != null)
                    itemContainer.AddChild(iconRect);
                itemContainer.AddChild(highlight);
                highlight.AddChild(eventLabel);
                AddChild(itemContainer);
            }

            if (data.Events.Any() == false)
            {
                var noneLabelContainer = new HBoxContainer();
                var noneLabelSpacer = new Control { CustomMinimumSize = new Vector2(25, 25) };
                var noneLabel = new Label { Text = Localization.Translate("NO_EVENTS_RECORDED") };

                noneLabelContainer.AddThemeConstantOverride("separation", 5);
                noneLabel.AddThemeFontOverride("normal_font", NormalFont.Font);
                noneLabel.AddThemeFontSizeOverride("normal_font_size", NormalFont.FontSize);

                noneLabelContainer.AddChild(noneLabelSpacer);
                noneLabelContainer.AddChild(noneLabel);

                AddChild(noneLabelContainer);
            }
        }
    }
}
