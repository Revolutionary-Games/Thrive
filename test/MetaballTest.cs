using System;
using Godot;

public class MetaballTest : Node
{
    private MulticellularMetaballDisplayer metaballDisplayer = null!;

    private DisplayLayout wantedLayout = DisplayLayout.Simple;
    private DisplayLayout currentLayout = DisplayLayout.None;

    private enum DisplayLayout
    {
        None,
        Simple,
    }

    public override void _Ready()
    {
        metaballDisplayer = GetNode<MulticellularMetaballDisplayer>("MulticellularMetaballDisplayer");
    }

    public override void _Process(float delta)
    {
        if (wantedLayout != currentLayout)
        {
            // Create the test layout for display
            currentLayout = wantedLayout;
            var layout = new MetaballLayout<Metaball>();

            switch (currentLayout)
            {
                case DisplayLayout.None:
                    break;
                case DisplayLayout.Simple:
                {
                    var cellType = CreateDummyCellType(Colors.Azure);
                    layout.Add(new MulticellularMetaball(cellType)
                    {
                        Parent = null,
                        Position = new Vector3(0, 0, 0),
                        Size = 1,
                    });

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            metaballDisplayer.DisplayFromLayout(layout);
        }
    }

    private CellType CreateDummyCellType(Color colour)
    {
        var simulationParameters = SimulationParameters.Instance;

        return new CellType(simulationParameters.GetMembrane("single"))
        {
            Colour = colour,
            Organelles =
                { new OrganelleTemplate(simulationParameters.GetOrganelleType("cytoplasm"), new Hex(0, 0), 0) },
        };
    }
}
