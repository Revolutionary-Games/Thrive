﻿using System;
using Godot;

/// <summary>
///   Various tests for metaballs
/// </summary>
public partial class MetaballTest : Node
{
#pragma warning disable CA2213
    private MacroscopicMetaballDisplayer metaballDisplayer = null!;
#pragma warning restore CA2213

    private DisplayLayout wantedLayout = DisplayLayout.PerformanceTest;
    private DisplayLayout currentLayout = DisplayLayout.None;

    private enum DisplayLayout
    {
        None,
        Simple,

        // TODO: add some kind of GUI to select the wanted mode
        PerformanceTest,
    }

    public override void _Ready()
    {
        metaballDisplayer = GetNode<MacroscopicMetaballDisplayer>("MulticellularMetaballDisplayer");
    }

    public override void _Process(double delta)
    {
        if (wantedLayout != currentLayout)
        {
            // Create the test layout for display
            currentLayout = wantedLayout;
            var layout = new MetaballLayout<MacroscopicMetaball>();

            switch (currentLayout)
            {
                case DisplayLayout.None:
                    break;
                case DisplayLayout.Simple:
                {
                    var cellType = CreateDummyCellType(Colors.Azure);
                    var root = new MacroscopicMetaball(cellType)
                    {
                        Parent = null,
                        Position = new Vector3(0, 0, 0),
                        Size = 1,
                    };

                    layout.Add(root);
                    layout.Add(new MacroscopicMetaball(cellType)
                    {
                        Parent = root,
                        Position = new Vector3(1, 0, 0),
                        Size = 0.5f,
                    });

                    break;
                }

                case DisplayLayout.PerformanceTest:
                {
                    var cellType1 = CreateDummyCellType(Colors.Azure);
                    var cellType2 = CreateDummyCellType(Colors.Chocolate);

                    var root = new MacroscopicMetaball(cellType1)
                    {
                        Parent = null,
                        Position = new Vector3(0, 0, 0),
                        Size = 1,
                    };
                    layout.Add(root);

                    bool type1 = true;

                    for (int x = -100; x < 100; ++x)
                    {
                        for (int z = -100; z < 100; ++z)
                        {
                            if (x == 0 && z == 0)
                                continue;

                            layout.Add(new MacroscopicMetaball(type1 ? cellType1 : cellType2)
                            {
                                // Could set the parents more intelligently here...
                                Parent = root,
                                Position = new Vector3(x, 0, z),
                                Size = 1.0f,
                            });

                            type1 = !type1;
                        }
                    }

                    GD.Print("Total metaballs: ", layout.Count);
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
