using Godot;

public class NumericFilterItem : BaseFilterItem
{
    [Export]
    public NodePath OperatorDropDownPath = null!;

    [Export]
    public NodePath ValueSliderPath = null!;

    private CustomDropDown operatorDropDown = null!;
    private HSlider valueSlider = null!;
    private NumericFilterDescription.Operators selectedOperator;

    public NumericFilterDescription.Operators Operator
    {
        get => selectedOperator;
        set
        {
            selectedOperator = value;

            if (operatorDropDown != null!)
                OperatorDropDownSelected((int)value);
        }
    }

    public double MinValue { get => valueSlider.MinValue; set => valueSlider.MinValue = value; }
    public double MaxValue { get => valueSlider.MaxValue; set => valueSlider.MaxValue = value; }
    public double Value { get => valueSlider.Value; set => valueSlider.Value = value; }

    public override void _Ready()
    {
        base._Ready();

        operatorDropDown = GetNode<CustomDropDown>(OperatorDropDownPath);
        valueSlider = GetNode<HSlider>(ValueSliderPath);

        // Initialize operator
        operatorDropDown.AddItem(TranslationServer.Translate("EQUALS_SIGN"), false, Colors.White);
        operatorDropDown.AddItem(TranslationServer.Translate("GREATER_THAN_SIGN"), false, Colors.White);
        operatorDropDown.AddItem(TranslationServer.Translate("NOT_LESS_THAN_SIGN"), false, Colors.White);
        operatorDropDown.AddItem(TranslationServer.Translate("LESS_THAN_SIGN"), false, Colors.White);
        operatorDropDown.AddItem(TranslationServer.Translate("NOT_GREATER_THAN_SIGN"), false, Colors.White);
        operatorDropDown.CreateElements();
        operatorDropDown.Popup.Connect("index_pressed", this, nameof(OperatorDropDownSelected));

        OperatorDropDownSelected((int)selectedOperator);
    }

    private void OperatorDropDownSelected(int index)
    {
        operatorDropDown.Text = operatorDropDown.Popup.GetItemText(index);
    }
}
