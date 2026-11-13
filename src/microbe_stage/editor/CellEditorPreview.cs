using System;
using System.Diagnostics;
using Arch.Core;
using Godot;

/// <summary>
///   Owns the cell editor's visual-only microbe simulation and synchronises its preview species.
///   The editor supplies creation and update callbacks that run synchronously when the preview needs them.
/// </summary>
public sealed class CellEditorPreview : IDisposable
{
    /// <summary>
    ///   Uses the membrane and organelle systems from the microbe stage for the preview
    /// </summary>
    private MicrobeVisualOnlySimulation? previewSimulation;

    private MicrobeSpecies? previewMicrobeSpecies;
    private Entity previewMicrobe = Entity.Null;

    // The Godot parent owns and frees this node.
#pragma warning disable CA2213
    private Node3D? cellPreviewVisualsRoot;
#pragma warning restore CA2213

    /// <summary>
    ///   Cleared only after the preview layout has been applied, so changing tabs does not rebuild it unnecessarily
    /// </summary>
    private bool organellePositionsAreDirty = true;

    private Func<MicrobeSpecies> createSpecies = null!;
    private Action<MicrobeSpecies> copyEditedProperties = null!;

    public bool IsInitialized => cellPreviewVisualsRoot != null;

    public void Initialize(Node3D parent, Func<MicrobeSpecies> createSpecies,
        Action<MicrobeSpecies> copyEditedProperties)
    {
        this.createSpecies = createSpecies;
        this.copyEditedProperties = copyEditedProperties;

        previewSimulation = new MicrobeVisualOnlySimulation();

        // Just to make *absolutely* sure this value cannot point to an old world
        previewMicrobe = Entity.Null;

        cellPreviewVisualsRoot = new Node3D
        {
            Name = "CellPreviewVisuals",
        };

        parent.AddChild(cellPreviewVisualsRoot);
        previewSimulation.Init(cellPreviewVisualsRoot);
    }

    public bool EnsureCreated(bool previewMode)
    {
        if (previewSimulation == null)
            throw new InvalidOperationException("Component needs to be initialized first");

        if (previewSimulation.Disposed)
            throw new InvalidOperationException("Preview world simulation has been disposed already");

#if DEBUG
        if (previewMicrobe == default)
        {
            GD.PrintErr("Preview microbe should not be default initialized");
            Debugger.Break();
        }
#endif

        if (previewMicrobe.IsAliveAndNotNull() && previewMicrobeSpecies != null)
            return false;

        if (cellPreviewVisualsRoot == null)
        {
            throw new InvalidOperationException("Editor component not initialized yet (cell visuals root missing)");
        }

        previewMicrobeSpecies = createSpecies();
        previewMicrobe = previewSimulation.CreateVisualisationMicrobe(previewMicrobeSpecies);
        if (!previewMicrobe.IsAliveAndNotNull())
            throw new InvalidOperationException("Failed to create preview microbe");

        // Set its initial visibility
        cellPreviewVisualsRoot.Visible = previewMode;

        return true;
    }

    public void SetVisibility(bool visible)
    {
        cellPreviewVisualsRoot?.Visible = visible;
    }

    public void MarkLayoutDirty()
    {
        organellePositionsAreDirty = true;
    }

    /// <summary>
    ///   Updates the membrane and organelle placement when preview mode is enabled.
    ///   Root visibility only controls simulation ticks, not these synchronous layout updates.
    /// </summary>
    public void UpdateLayoutIfNeeded(bool previewMode)
    {
        if (previewMicrobeSpecies == null)
            return;

        // Don't redo the preview cell when not in the preview mode to avoid unnecessary lags
        if (!previewMode || !organellePositionsAreDirty)
            return;

        copyEditedProperties(previewMicrobeSpecies);

        // Intentionally force it to not be bacteria to show it at full size
        previewMicrobeSpecies.IsBacteria = false;

        // This is now just for applying changes in the species to the preview cell
        previewSimulation!.ApplyNewVisualisationMicrobeSpecies(previewMicrobe, previewMicrobeSpecies);

        organellePositionsAreDirty = false;
    }

    public void ApplyRigidity(float value)
    {
        if (previewMicrobeSpecies == null)
            return;

        previewMicrobeSpecies.MembraneRigidity = value;

        if (previewMicrobe.IsAliveAndNotNull())
            previewSimulation!.ApplyMicrobeRigidity(previewMicrobe, previewMicrobeSpecies.MembraneRigidity);
    }

    public void ApplyColour(Color value)
    {
        if (previewMicrobeSpecies == null)
            return;

        previewMicrobeSpecies.SpeciesColour = value;

        if (previewMicrobe.IsAliveAndNotNull())
            previewSimulation!.ApplyMicrobeColour(previewMicrobe, previewMicrobeSpecies.SpeciesColour);
    }

    public void ApplyMembrane(MembraneType membrane)
    {
        if (previewMicrobeSpecies == null)
            return;

        previewMicrobeSpecies.MembraneType = membrane;

        if (previewMicrobe.IsAliveAndNotNull())
            previewSimulation!.ApplyMicrobeMembraneType(previewMicrobe, membrane);
    }

    /// <summary>
    ///   Ticks the visual simulation when its root is visible. The editor controls its own processing gate.
    /// </summary>
    public void ProcessIfVisible(double delta)
    {
        if (cellPreviewVisualsRoot!.Visible)
            previewSimulation!.ProcessAll((float)delta);
    }

    public void Dispose()
    {
        previewSimulation?.Dispose();
        previewMicrobe = Entity.Null;
    }
}
