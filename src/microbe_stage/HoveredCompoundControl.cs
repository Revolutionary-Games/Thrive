using Godot;

public class HoveredCompoundControl : HBoxContainer
{
#pragma warning disable CA2213
    private Label compoundName = null!;
    private Label compoundValue = null!;
#pragma warning restore CA2213

    public HoveredCompoundControl(Compound compound)
    {
        Compound = compound;
    }

    public Compound Compound { get; }

    public string? Category
    {
        get => compoundValue.Text;
        set => compoundValue.Text = value;
    }

    public Color CategoryColor
    {
        get => compoundValue.Modulate;
        set => compoundValue.Modulate = value;
    }

    public override void _Ready()
    {
        compoundName = new Label();
        compoundValue = new Label();

        MouseFilter = MouseFilterEnum.Ignore;
        TextureRect compoundIcon = GUICommon.Instance.CreateCompoundIcon(Compound.InternalName, 20, 20);
        compoundName.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
        compoundName.Text = Compound.Name;
        AddChild(compoundIcon);
        AddChild(compoundName);
        AddChild(compoundValue);
        Visible = false;
    }

    public void UpdateTranslation()
    {
        compoundName.Text = Compound.Name;
    }
}
