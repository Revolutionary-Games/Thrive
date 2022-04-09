﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public class TimelineTab : PanelContainer
{
    [Export]
    public NodePath EventsContainerPath = null!;

    [Export]
    public NodePath ScrollContainerPath = null!;

    [Export]
    public NodePath LocalFilterButtonPath = null!;

    [Export]
    public NodePath GlobalFilterButtonPath = null!;

    private readonly PackedScene customRichTextLabelScene = GD.Load<PackedScene>(
        "res://src/gui_common/CustomRichTextLabel.tscn");

    private readonly StyleBoxTexture eventHighlightStyleBox = GD.Load<StyleBoxTexture>(
        "res://src/microbe_stage/editor/TimelineEventHighlight.tres");

    private VBoxContainer eventsContainer = null!;
    private ScrollContainer scrollContainer = null!;
    private Button localFilterButton = null!;
    private Button globalFilterButton = null!;

    private Filters eventFilter = Filters.Local;

    private List<TimelineSection>? cachedLocalTimelineElements;
    private List<TimelineSection>? cachedGlobalTimelineElements;

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
        eventsContainer = GetNode<VBoxContainer>(EventsContainerPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);
        localFilterButton = GetNode<Button>(LocalFilterButtonPath);
        globalFilterButton = GetNode<Button>(GlobalFilterButtonPath);
    }

    public void UpdateTimeline(IEditorReportData editor, Patch? selectedPatch, Patch? patch = null)
    {
        if (editor.CurrentGame == null)
            throw new ArgumentException($"Editor must be initialized ({nameof(IEditorReportData.CurrentGame)} is null");

        eventsContainer.FreeChildren();

        cachedGlobalTimelineElements = new List<TimelineSection>();
        cachedLocalTimelineElements = new List<TimelineSection>();

        foreach (var entry in editor.CurrentGame.GameWorld.EventsLog)
        {
            var section = new TimelineSection(
                customRichTextLabelScene, eventHighlightStyleBox, (entry.Key, entry.Value));

            cachedGlobalTimelineElements.Add(section);
            eventsContainer.AddChild(section);
        }

        var targetPatch = patch ?? selectedPatch ?? editor.CurrentPatch;

        for (int i = targetPatch.History.Count - 1; i >= 0; i--)
        {
            var snapshot = targetPatch.History[i];
            var section = new TimelineSection(
                customRichTextLabelScene, eventHighlightStyleBox, (snapshot.TimePeriod, snapshot.EventsLog));

            cachedLocalTimelineElements.Add(section);
            eventsContainer.AddChild(section);
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
            anchorRect = last != null ? last.HeaderGlobalRect : new Rect2(Vector2.Zero, Vector2.Zero);
        }
        else if (eventFilter == Filters.Local)
        {
            var last = cachedLocalTimelineElements?.LastOrDefault();
            anchorRect = last != null ? last.HeaderGlobalRect : new Rect2(Vector2.Zero, Vector2.Zero);
        }

        var diff = Mathf.Max(Mathf.Min(anchorRect.Position.y, scrollRect.Position.y), anchorRect.Position.y +
            anchorRect.Size.y - scrollRect.Size.y + (scrollRect.Size.y - anchorRect.Size.y));

        scrollContainer.ScrollVertical += (int)(diff - scrollRect.Position.y);
    }

    private void ApplyEventsFilter()
    {
        switch (EventFilter)
        {
            case Filters.Global:
                cachedGlobalTimelineElements?.ForEach(e => e.Show());
                cachedLocalTimelineElements?.ForEach(e => e.Hide());
                globalFilterButton.Pressed = true;
                break;
            case Filters.Local:
                cachedGlobalTimelineElements?.ForEach(e => e.Hide());
                cachedLocalTimelineElements?.ForEach(e => e.Show());
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

        private Control? headerContainer;

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
