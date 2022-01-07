using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public class TimelineTab : PanelContainer
{
    [Export]
    public NodePath EventsContainerPath;

    [Export]
    public NodePath ScrollContainerPath;

    [Export]
    public NodePath FilterButtonsMarginContainerPath;

    private MicrobeEditor editor;
    private PatchMapDrawer patchMapDrawer;

    private VBoxContainer eventsContainer;
    private ScrollContainer scrollContainer;
    private MarginContainer filterButtonsMarginContainer;

    private Filters eventFilter = Filters.Local;

    private List<TimelineSection> cachedLocalTimelineElements;
    private List<TimelineSection> cachedGlobalTimelineElements;

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

    public Patch SelectedPatch { get; set; }

    public void Init(MicrobeEditor editor, PatchMapDrawer drawer)
    {
        this.editor = editor;
        patchMapDrawer = drawer;
    }

    public override void _Ready()
    {
        eventsContainer = GetNode<VBoxContainer>(EventsContainerPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);
        filterButtonsMarginContainer = GetNode<MarginContainer>(FilterButtonsMarginContainerPath);
    }

    public void UpdateTimeline()
    {
        eventsContainer.FreeChildren();

        cachedGlobalTimelineElements = new List<TimelineSection>();
        cachedLocalTimelineElements = new List<TimelineSection>();

        foreach (var entry in editor.CurrentGame.GameWorld.EventsLog)
        {
            var section = new TimelineSection((entry.Key, entry.Value));

            cachedGlobalTimelineElements.Add(section);
            eventsContainer.AddChild(section);
        }

        for (int i = SelectedPatch.History.Count - 1; i >= 0; i--)
        {
            var snapshot = SelectedPatch.History[i];
            var section = new TimelineSection((snapshot.TimePeriod, snapshot.EventsLog));

            cachedLocalTimelineElements.Add(section);
            eventsContainer.AddChild(section);
        }

        ApplyEventsFilter();

        Invoke.Instance.Queue(UpdateFilterButtonsMargin);
    }

    public void TimelineAutoScrollToCurrentTimePeriod()
    {
        var scrollRect = scrollContainer.GetGlobalRect();
        var anchorRect = new Rect2();

        if (eventFilter == Filters.Global)
        {
            var last = cachedGlobalTimelineElements.LastOrDefault();
            anchorRect = last != null ? last.HeaderGlobalRect : new Rect2();
        }
        else if (eventFilter == Filters.Local)
        {
            var last = cachedLocalTimelineElements.LastOrDefault();
            anchorRect = last != null ? last.HeaderGlobalRect : new Rect2();
        }

        var diff = Mathf.Max(Mathf.Min(anchorRect.Position.y, scrollRect.Position.y), anchorRect.Position.y +
            anchorRect.Size.y - scrollRect.Size.y + (scrollRect.Size.y - anchorRect.Size.y));

        scrollContainer.ScrollVertical += (int)(diff - scrollRect.Position.y);

        UpdateFilterButtonsMargin();
    }

    private void ApplyEventsFilter()
    {
        switch (EventFilter)
        {
            case Filters.Global:
                cachedGlobalTimelineElements?.ForEach(e => e.Show());
                cachedLocalTimelineElements?.ForEach(e => e.Hide());
                break;
            case Filters.Local:
                cachedGlobalTimelineElements?.ForEach(e => e.Hide());
                cachedLocalTimelineElements?.ForEach(e => e.Show());
                break;
            default:
                throw new Exception("Not a valid event filter");
        }

        Invoke.Instance.Queue(TimelineAutoScrollToCurrentTimePeriod);
    }

    private void UpdateFilterButtonsMargin()
    {
        filterButtonsMarginContainer.AddConstantOverride(
            "margin_right", scrollContainer.GetVScrollbar().Visible ? 15 : 0);
    }

    private void OnFilterSelected(int index)
    {
        switch (index)
        {
            case 0:
                EventFilter = Filters.Global;
                break;
            case 1:
                EventFilter = Filters.Local;
                break;
        }
    }

    private class TimelineSection : VBoxContainer
    {
        private Control headerContainer;

        private PackedScene customRtlScene;
        private StyleBoxTexture eventHighlightStylebox;

        private (double TimePeriod, List<GameEventDescription> Events) data;

        public Rect2 HeaderGlobalRect => headerContainer.GetGlobalRect();

        public TimelineSection((double TimePeriod, List<GameEventDescription> Events) data)
        {
            this.data = data;
        }

        public override void _Ready()
        {
            customRtlScene = GD.Load<PackedScene>("res://src/gui_common/CustomRichTextLabel.tscn");
            eventHighlightStylebox = GD.Load<StyleBoxTexture>(
                "res://src/microbe_stage/editor/TimelineEventHighlight.tres");

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
                var iconRect = new TextureRect
                {
                    RectMinSize = new Vector2(25, 25),
                    SizeFlagsVertical = (int)SizeFlags.ShrinkCenter,
                    Texture = GUICommon.LoadGuiAsset(entry.IconPath),
                    Expand = true,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                };

                var highlight = new PanelContainer
                {
                    SelfModulate = entry.Highlighted ? Colors.White : Colors.Transparent,
                    SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                };

                highlight.AddStyleboxOverride("panel", eventHighlightStylebox);
                itemContainer.AddConstantOverride("separation", 5);

                var eventLabel = customRtlScene.Instance<CustomRichTextLabel>();
                eventLabel.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
                eventLabel.ExtendedBbcode = entry.Description.ToString();
                eventLabel.FitContentHeight = true;

                eventLabel.AddFontOverride("normal_font", GetFont("jura_almost_smaller", "Fonts"));
                eventLabel.AddFontOverride("bold_font", GetFont("jura_demibold_almost_smaller", "Fonts"));
                eventLabel.AddConstantOverride("line_separation", 0);

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
