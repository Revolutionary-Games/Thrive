using Godot;

/// <summary>
///   Adds radar capability to a cell
/// </summary>
public class ChemoreceptorComponent : ExternallyPositionedComponent
{
    private Compound targetCompound;
    private float searchRange;
    private float searchAmount;
    private Color lineColour;
    private bool isActive;

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        // For now only player's chemoreceptor does anything during gameplay
        if (!isActive)
            return;

        organelle.ParentMicrobe.ReportActiveChemereception(targetCompound, searchRange, searchAmount, lineColour);
    }

    protected override void CustomAttach()
    {
        isActive = organelle.ParentMicrobe.IsPlayerMicrobe;

        // TODO: get reference to organelle upgrade data here to find what we should look for

        targetCompound = SimulationParameters.Instance.GetCompound("glucose");
        searchRange = Constants.CHEMORECEPTOR_RANGE_DEFAULT;
        searchAmount = Constants.CHEMORECEPTOR_AMOUNT_DEFAULT;
        lineColour = new Color(0, 1, 0);
    }

    protected override bool NeedsUpdateAnyway()
    {
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/2906
        return organelle.OrganelleGraphics.Transform.basis == Transform.Identity.basis;
    }

    protected override void OnPositionChanged(Quat rotation, float angle, Vector3 membraneCoords)
    {
        organelle.OrganelleGraphics.Transform = new Transform(rotation, membraneCoords);
    }
}

public class ChemoreceptorComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new ChemoreceptorComponent();
    }

    public void Check(string name)
    {
    }
}
