using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Main class of the microbe editor
/// </summary>
public class MicrobeEditor : Node
{
    /// <summary>
    ///   The new to set on the species after exiting
    /// </summary>
    public string NewName;

    /// <summary>
    ///   Selected colour for the species
    /// </summary>

    private int symmetry = 0;

    private MicrobeCamera camera;
    private Node world;
    private MicrobeEditorGUI gui;

    /// <summary>
    ///   Where all user actions will  be registered
    /// </summary>
    private ActionHistory<EditorAction> history;

    private Material invalidMaterial;
    private Material validMaterial;
    private PackedScene hexScene;
    private PackedScene modelScene;

    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    private Patch targetPatch = null;

    /// <summary>
    ///   When false the player is no longer allowed to move patches (other than going back to where they were at the
    ///   start)
    /// </summary>
    private bool canStillMove;

    private Patch playerPatchOnEntry;

    /// <summary>
    ///   The hexes that are positioned under the cursor to show where
    ///   the player is about to place something.
    /// </summary>
    private List<MeshInstance> hoverHexes;

    /// <summary>
    ///   The organelle models that are positioned to show what the
    ///   player is about to place.
    /// </summary>
    private List<SceneDisplayer> hoverOrganelles;

    /// <summary>
    ///   This is used to keep track of used hover organelles
    /// </summary>
    private int usedHoverHex = 0;
    private int usedHoverOrganelle = 0;

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    private MicrobeSpecies editedSpecies;

    /// <summary>
    ///   This is a global assesment if the currently being placed
    ///   organelle is valid (if not all hover hexes will be shown as
    ///   invalid)
    /// </summary>
    private bool isPlacementProbablyValid = false;

    /// <summary>
    ///   This is the container that has the edited organelles in
    ///   it. This is populated when entering and used to update the
    ///   player's species template on exit.
    /// </summary>
    private OrganelleLayout<OrganelleTemplate> editedMicrobeOrganelles;

    // This is the already placed hexes

    /// <summary>
    ///   This is the hexes for editedMicrobeorganelles
    /// </summary>
    private List<MeshInstance> placedHexes;

    /// <summary>
    ///   This is the organelle models for editedMicrobeorganelles
    /// </summary>
    private List<SceneDisplayer> placedModels;

    /// <summary>
    ///   True once auto-evo (and possibly other stuff) we need to wait for is ready
    /// </summary>
    private bool ready = false;

    private int organelleRot = 0;

    public MicrobeCamera Camera
    {
        get
        {
            return camera;
        }
    }

    /// <summary>
    ///   The selected membrane rigidity
    /// </summary>
    public float Rigidity { get; private set; }

    /// <summary>
    ///   Selected membrane type for the species
    /// </summary>
    public MembraneType Membrane { get; private set; }

    /// <summary>
    ///   The name of organelle type that is selected to be placed
    /// </summary>
    public string ActiveActionName { get; set; }

    /// <summary>
    ///   The number of mutation points left
    /// </summary>
    public int MutationPoints { get; private set; }

    /// <summary>
    ///   0 is no symmetry, 1 is x-axis symmetry, 2 is 4-way symmetry, and 3 is 6-way symmetry.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: change to enum
    ///   </para>
    /// </remarks>
    public int Symmetry
    {
        get
        {
            return symmetry;
        }
        set
        {
            if (value < 0 || value > 3)
                throw new ArgumentException("invalid value for symmetry");

            symmetry = value;
        }
    }

    /// <summary>
    ///   When true nothing costs MP
    /// </summary>
    public bool FreeBuilding { get; private set; } = false;

    /// <summary>
    ///   Hover hexes and models are only shown if this is true
    /// </summary>
    public bool ShowHover { get; set; } = false;

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    public GameProperties CurrentGame { get; set; }

    /// <summary>
    ///   If set the editor returns to this stage. The CurrentGame
    ///   should be shared with this stage. If not set returns to a new microbe stage
    /// </summary>
    public MicrobeStage ReturnToStage { get; set; }

    public bool HasNucleus
    {
        get
        {
            foreach (var organelle in editedMicrobeOrganelles.Organelles)
            {
                if (organelle.Definition.InternalName == "nucleus")
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    ///   Number of organelles in the microbe
    /// </summary>
    public int MicrobeSize
    {
        get
        {
            return editedMicrobeOrganelles.Organelles.Count;
        }
    }

    /// <summary>
    ///   Number of hexes in the microbe
    /// </summary>
    public int MicrobeHexSize
    {
        get
        {
            int result = 0;

            foreach (var organelle in editedMicrobeOrganelles.Organelles)
            {
                result += organelle.Definition.HexCount;
            }

            return result;
        }
    }

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    public Patch CurrentPatch
    {
        get
        {
            return targetPatch ?? playerPatchOnEntry;
        }
    }

    public override void _Ready()
    {
        camera = GetNode<MicrobeCamera>("PrimaryCamera");
        world = GetNode("World");
        gui = GetNode<MicrobeEditorGUI>("MicrobeEditorGUI");

        invalidMaterial = GD.Load<Material>(
            "res://src/microbe_stage/editor/InvalidHex.material");
        validMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        camera.ObjectToFollow = GetNode<Spatial>("CameraLookAt");

        gui.Init(this);

        OnEnterEditor();
    }

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    public void OnEnterEditor()
    {
        // Clear old stuff in the world
        foreach (Node node in world.GetChildren())
        {
            node.Free();
        }

        // Let go of old resources
        hoverHexes = new List<MeshInstance>();
        hoverOrganelles = new List<SceneDisplayer>();

        history = new ActionHistory<EditorAction>();

        // Create new hover hexes. See the TODO comment in _Process
        // This seems really cluttered, there must be a better way.
        for (int i = 0; i < Constants.MAX_HOVER_HEXES; ++i)
        {
            hoverHexes.Add(CreateEditorHex());
        }

        for (int i = 0; i < Constants.MAX_SYMMETRY; ++i)
        {
            hoverOrganelles.Add(CreateEditorOrganelle());
        }

        // Start a new game if no game has been started
        if (CurrentGame == null)
        {
            if (ReturnToStage != null)
                throw new Exception("stage to return to should have set our current game");

            GD.Print("Starting a new game for the microbe editor");
            CurrentGame = GameProperties.StartNewMicrobeGame();
        }

        InitEditor();

        StartMusic();
    }

    /// <summary>
    ///   Applies the changes done and exists the editor
    /// </summary>
    public void OnFinishEditing()
    {
        GD.Print("MicrobeEditor: applying changes to edited Species");

        if (ReturnToStage == null)
        {
            GD.Print("Creating new microbe stage as there isn't one yet");

            var scene = GD.Load<PackedScene>("res://src/microbe_stage/MicrobeStage.tscn");

            ReturnToStage = (MicrobeStage)scene.Instance();
            ReturnToStage.CurrentGame = CurrentGame;
        }

        // Apply changes to the species organelles

        // It is easiest to just replace all
        editedSpecies.Organelles.RemoveAll();

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            editedSpecies.Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        // Update bacteria status
        editedSpecies.IsBacteria = !HasNucleus;

        GD.Print("MicrobeEditor: updated organelles for species: ",
            editedSpecies.FormattedName);

        // Update name
        NewName = gui.GetNewSpeciesName();
        var splits = NewName.Split(" ");
        if (splits.Length == 2)
        {
            editedSpecies.Genus = splits[0];
            editedSpecies.Epithet = splits[1];

            GD.Print("MicrobeEditor: edited species name is now ",
                editedSpecies.FormattedName);
        }
        else
        {
            GD.PrintErr("MicrobeEditor: Invalid newName: ", NewName);
        }

        // Update membrane
        editedSpecies.MembraneType = Membrane;
        editedSpecies.Colour = gui.GetMembraneColor();
        editedSpecies.MembraneRigidity = Rigidity;

        // Move patches
        if (targetPatch != null)
        {
            GD.Print("MicrobeEditor: applying player move to patch: ", targetPatch.Name);
            CurrentGame.GameWorld.Map.CurrentPatch = targetPatch;
        }

        var parent = GetParent();
        parent.RemoveChild(this);
        parent.AddChild(ReturnToStage);
        ReturnToStage.OnReturnFromEditor();

        QueueFree();
    }

    public void StartMusic()
    {
        Jukebox.Instance.PlayingCategory = "MicrobeEditor";
        Jukebox.Instance.Resume();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("e_rotate_right"))
        {
            RotateRight();
        }

        if (@event.IsActionPressed("e_rotate_left"))
        {
            RotateLeft();
        }

        if (@event.IsActionPressed("e_redo"))
        {
            Redo();
        }
        else if (@event.IsActionPressed("e_undo"))
        {
            Undo();
        }

        if (@event.IsActionPressed("e_primary"))
        {
            PlaceOrganelle();
        }

        if (@event.IsActionPressed("e_secondary"))
        {
            RemoveOrganelle();
        }
    }

    public override void _Process(float delta)
    {
        if (!ready)
        {
            if (!CurrentGame.GameWorld.IsAutoEvoFinished())
            {
                gui.SetLoadingText("Loading Microbe Editor", "Waiting for auto-evo: " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }
            else
            {
                OnEditorReady();
            }
        }

        UpdateEditor(delta);
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    public void CalculateEnergyBalanceWithOrganellesAndMembraneType(
        List<OrganelleTemplate> organelles, MembraneType membrane, Patch patch = null)
    {
        if (patch == null)
        {
            patch = CurrentPatch;
        }

        gui.UpdateEnergyBalance(ProcessSystem.ComputeEnergyBalance(organelles.Select((i) => i.Definition), patch.Biome,
                membrane));
    }

    /// <summary>
    ///   Calculates the effectiveness of organelles in the current or
    ///   given patch
    /// </summary>
    public void CalculateOrganelleEffectivenessInPatch(Patch patch = null)
    {
        if (patch == null)
        {
            patch = CurrentPatch;
        }

        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        var result = ProcessSystem.ComputeOrganelleProcessEfficiencies(organelles, patch.Biome);

        gui.UpdateOrganelleEfficiencies(result);
    }

    /// <summary>
    ///   Wipes clean the current cell.
    /// </summary>
    public void CreateNewMicrobe()
    {
        if (!FreeBuilding)
            throw new InvalidOperationException("can't reset cell when not freebuilding");

        var previousMP = MutationPoints;
        var oldEditedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>();

        foreach (var organelle in editedMicrobeOrganelles)
        {
            oldEditedMicrobeOrganelles.Add(organelle);
        }

        var action = new EditorAction(this, 0,
            redo =>
            {
                MutationPoints = Constants.BASE_MUTATION_POINTS;
                editedMicrobeOrganelles.RemoveAll();
                editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
                    new Hex(0, 0), 0));
            },
            undo =>
            {
                editedMicrobeOrganelles.RemoveAll();
                MutationPoints = previousMP;

                foreach (var organelle in oldEditedMicrobeOrganelles)
                {
                    editedMicrobeOrganelles.Add(organelle);
                }
            });

        EnqueueAction(action);
    }

    public void Redo()
    {
        history.Redo();

        UpdateUndoRedoButtons();
    }

    public void Undo()
    {
        history.Undo();

        UpdateUndoRedoButtons();
    }

    public void PlaceOrganelle()
    {
        if (ActiveActionName != null)
            AddOrganelle(ActiveActionName);
    }

    public void RotateRight()
    {
        organelleRot = (organelleRot + 1) % 6;
    }

    public void RotateLeft()
    {
        --organelleRot;

        if (organelleRot < 0)
            organelleRot = 5;
    }

    public void SetMembrane(Membrane membrane)
    {
        if (Membrane.Equals(membrane))
            return;

        throw new NotImplementedException();

        // int cost = SimulationParameters::membraneRegistry().getTypeData(
        // string(vars.GetSingleValueByName("membrane"))).editorCost;

        // EditorAction@ action = EditorAction(cost,
        //     // redo
        //     function(EditorAction@ action, MicrobeEditor@ editor){
        //         editor.membrane = MembraneTypeId(action.data["membrane"]);
        //         GenericEvent@ event = GenericEvent("MicrobeEditorMembraneUpdated");
        //         NamedVars@ vars = event.GetNamedVars();
        //         vars.AddValue(ScriptSafeVariableBlock("membrane",
        //         SimulationParameters::membraneRegistry().getInternalName(editor.membrane)));
        //         GetEngine().GetEventHandler().CallEvent(event);
        //         // Calculate and send energy balance to the GUI
        //         calculateEnergyBalanceWithOrganellesAndMembraneType(
        //         editor.editedMicrobeOrganelles, editor.membrane, editor.targetPatch);
        //         // not using _onEditedCellChange due to visuals not needing update
        //     },
        //     // undo
        //     function(EditorAction@ action, MicrobeEditor@ editor){
        //         editor.membrane = MembraneTypeId(action.data["prevMembrane"]);
        //         GenericEvent@ event = GenericEvent("MicrobeEditorMembraneUpdated");
        //         NamedVars@ vars = event.GetNamedVars();
        //         vars.AddValue(ScriptSafeVariableBlock("membrane",
        //         SimulationParameters::membraneRegistry().getInternalName(editor.membrane)));
        //         GetEngine().GetEventHandler().CallEvent(event);
        //         // Calculate and send energy balance to the GUI
        //         calculateEnergyBalanceWithOrganellesAndMembraneType(
        //         editor.editedMicrobeOrganelles, editor.membrane, editor.targetPatch);
        //     }
        // );

        // action.data["membrane"] = SimulationParameters::membraneRegistry().getTypeId(
        //     string(vars.GetSingleValueByName("membrane")));
        // action.data["prevMembrane"] = membrane;

        // enqueueAction(action);
    }

    public void SetRigidity(float rigidity)
    {
        if (Math.Abs(Rigidity - rigidity) < MathUtils.EPSILON)
            return;

        var cost = (int)(Math.Abs(rigidity - Rigidity) / 2 * 100);

        if (cost > 0)
        {
            if (cost > MutationPoints)
            {
                rigidity = Rigidity + (rigidity < Rigidity ? -MutationPoints : MutationPoints) * 2 / 100.0f;
                cost = MutationPoints;
            }

            var newRigidity = rigidity;
            var prevRigidity = Rigidity;

            var action = new EditorAction(this, cost,
                redo =>
                {
                    Rigidity = newRigidity;
                    gui.UpdateRigiditySlider(Rigidity, MutationPoints);
                    gui.UpdateSpeed(CalculateSpeed());
                },
                undo =>
                {
                    Rigidity = prevRigidity;
                    gui.UpdateRigiditySlider(Rigidity, MutationPoints);
                    gui.UpdateSpeed(CalculateSpeed());
                });

            EnqueueAction(action);
        }
    }

    /// <summary>
    ///   Removes organelles under the cursor
    /// </summary>
    public void RemoveOrganelle()
    {
        GetMouseHex(out int q, out int r);

        switch (Symmetry)
        {
            case 0:
                {
                    RemoveOrganelleAt(new Hex(q, r));
                }

                break;
            case 1:
                {
                    RemoveOrganelleAt(new Hex(q, r));

                    if (q != -1 * q || r != r + q)
                    {
                        RemoveOrganelleAt(new Hex(-1 * q, r + q));
                    }
                }

                break;
            case 2:
                {
                    RemoveOrganelleAt(new Hex(q, r));

                    if (q != -1 * q || r != r + q)
                    {
                        RemoveOrganelleAt(new Hex(-1 * q, r + q));
                        RemoveOrganelleAt(new Hex(-1 * q, -1 * r));
                        RemoveOrganelleAt(new Hex(q, -1 * (r + q)));
                    }
                }

                break;
            case 3:
                {
                    RemoveOrganelleAt(new Hex(q, r));

                    if (q != -1 * q || r != r + q)
                    {
                        RemoveOrganelleAt(new Hex(-1 * r, r + q));
                        RemoveOrganelleAt(new Hex(-1 * (r + q), q));
                        RemoveOrganelleAt(new Hex(-1 * q, -1 * r));
                        RemoveOrganelleAt(new Hex(r, -1 * (r + q)));
                        RemoveOrganelleAt(new Hex(r, -1 * (r + q)));
                        RemoveOrganelleAt(new Hex(r + q, -1 * q));
                    }
                }

                break;
        }
    }

    public float CalculateSpeed()
    {
        float finalSpeed = 0;
        int flagCount = 0;
        float lengthMicrobe = 0;

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            lengthMicrobe += organelle.Definition.HexCount;

            // TODO: this should be changed to instead check for the
            // movement component presence
            if (organelle.Definition.InternalName == "flagellum")
                flagCount++;
        }

        // This is complex, I know
        finalSpeed = (Constants.CELL_BASE_THRUST +
            ((flagCount / (lengthMicrobe - flagCount)) * Constants.FLAGELLA_BASE_FORCE) +
            (Constants.CELL_DRAG_MULTIPLIER -
                (Constants.CELL_SIZE_DRAG_MULTIPLIER * lengthMicrobe))) *
            (Membrane.MovementFactor -
            (Rigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
        return finalSpeed;
    }

    /// <summary>
    ///   Returns true when the player is allowed to move to the specified patch
    /// </summary>
    public bool IsPatchMoveValid(Patch patch)
    {
        if (patch == null)
            return false;

        var from = CurrentPatch;

        // Can't go to the patch you are in
        if (from == patch)
            return false;

        // Can return to the patch the player started in, as a way to "undo" the change
        if (patch == playerPatchOnEntry)
            return true;

        // Can't move if out of moves
        if (!canStillMove)
            return false;

        // Need to have a connection to move
        foreach (var adjacent in from.Adjacent)
        {
            if (adjacent == patch)
                return true;
        }

        return false;
    }

    public void SetPlayerPatch(Patch patch)
    {
        if (!IsPatchMoveValid(patch))
            throw new ArgumentException("can't move to the specified patch");

        // One move per editor cycle allowed, unless freebuilding
        if (!FreeBuilding)
            canStillMove = false;

        if (patch == playerPatchOnEntry)
        {
            targetPatch = null;

            // Undoing the move, restores the move
            canStillMove = true;
        }
        else
        {
            targetPatch = patch;
        }

        gui.UpdatePlayerPatch(targetPatch);
        UpdatePatchBackgroundImage();
        CalculateOrganelleEffectivenessInPatch(targetPatch);
    }

    /// <summary>
    ///   Changes the number of mutation points left. Should only be called by EditorAction
    /// </summary>
    internal void ChangeMutationPoints(int change)
    {
        if (FreeBuilding)
            return;

        MutationPoints = (MutationPoints + change).Clamp(0, Constants.BASE_MUTATION_POINTS);
    }

    /// <summary>
    ///   Combined old editor init and activate method
    /// </summary>
    private void InitEditor()
    {
        // For now we only show a loading screen if auto-evo is not ready yet
        if (!CurrentGame.GameWorld.IsAutoEvoFinished())
        {
            ready = false;
            gui.SetLoadingStatus(true);
            gui.SetLoadingText("Loading Microbe Editor", CurrentGame.GameWorld.GetAutoEvoRun().Status);
        }
        else
        {
            OnEditorReady();
        }

        MutationPoints = Constants.BASE_MUTATION_POINTS;
        editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        organelleRot = 0;

        Symmetry = 0;
        gui.ResetSymmetryButton();

        UpdateUndoRedoButtons();

        // The world is reset each time so these are gone
        placedHexes = new List<MeshInstance>();
        placedModels = new List<SceneDisplayer>();

        // Check generation and set it here.

        CurrentGame.SetBool("edited_microbe", true);

        if (CurrentGame.FreeBuild)
        {
            GD.Print("Editor going to freebuild mode because player has activated freebuild");
            FreeBuilding = true;
        }
        else
        {
            // Make sure freebuilding doesn't get stuck on
            FreeBuilding = false;
        }

        // Sent freebuild value to GUI
        gui.NotifyFreebuild(FreeBuilding);

        playerPatchOnEntry = CurrentGame.GameWorld.Map.CurrentPatch;

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch();

        // Reset this, GUI will tell us to enable it again
        ShowHover = false;
        targetPatch = null;

        canStillMove = true;

        UpdatePatchBackgroundImage();

        gui.SetMap(CurrentGame.GameWorld.Map);

        var playerSpecies = CurrentGame.GameWorld.PlayerSpecies;

        SetupEditedSpecies(playerSpecies as MicrobeSpecies);

        gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);
    }

    private void SetupEditedSpecies(MicrobeSpecies species)
    {
        if (species == null)
            throw new NullReferenceException("didn't find edited species");

        editedSpecies = species;

        // We need to set the membrane type here so the ATP balance
        // bar can take it into account (the bar is updated when
        // organelles are added)
        Membrane = species.MembraneType;

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in species.Organelles.Organelles)
        {
            editedMicrobeOrganelles.Add((OrganelleTemplate)organelle.Clone());
        }

        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe, genes: ", species.StringCode);

        // Update GUI buttons now that we have correct organelles
        gui.UpdateGuiButtonStatus(HasNucleus);

        // Create a mutated version of the current species code to compete against the player
        CreateMutatedSpeciesCopy(species);

        // Reset to cytoplasm if nothing is selected
        if (ActiveActionName == null)
        {
            gui.OnOrganelleToPlaceSelected("cytoplasm");
        }

        NewName = species.FormattedName;
        Rigidity = species.MembraneRigidity;

        gui.SetSpeciesInfo(NewName, Membrane, species.Colour, Rigidity);

        species.Generation += 1;
        gui.UpdateGeneration(species.Generation);
    }

    private void CreateMutatedSpeciesCopy(Species species)
    {
        var newSpecies = CurrentGame.GameWorld.CreateMutatedSpecies(species);

        var random = new Random();

        var population = random.Next(Constants.INITIAL_SPLIT_POPULATION_MIN,
            Constants.INITIAL_SPLIT_POPULATION_MAX + 1);

        if (!CurrentGame.GameWorld.Map.CurrentPatch.AddSpecies(newSpecies, population))
        {
            GD.PrintErr("Failed to create a mutated version of the edited species");
        }
    }

    private void UpdateEditor(float delta)
    {
        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this and instead only updated
        // the hover hexes and organelles when there is some change to them
        foreach (var hex in hoverHexes)
        {
            hex.Translation = new Vector3(0, 0, 0);
            hex.Visible = false;
        }

        foreach (var organelle in hoverOrganelles)
        {
            organelle.Translation = new Vector3(0, 0, 0);
            organelle.Visible = false;
        }

        // This is also highly non-optimal to update the hex locations
        // and materials all the time

        // Reset colour of each already placed hex
        foreach (var hex in placedHexes)
        {
            hex.MaterialOverride = validMaterial;
        }

        usedHoverHex = 0;
        usedHoverOrganelle = 0;

        // Show the organelle that is about to be placed
        if (ActiveActionName != null && ShowHover)
        {
            GetMouseHex(out int q, out int r);

            // Can place stuff at all?
            isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

            switch (Symmetry)
            {
                case 0:
                    RenderHighlightedOrganelle(1, q, r, organelleRot);
                    break;
                case 1:
                    RenderHighlightedOrganelle(1, q, r, organelleRot);
                    RenderHighlightedOrganelle(2, -1 * q, r + q, 6 + (-1 * organelleRot));
                    break;
                case 2:
                    RenderHighlightedOrganelle(1, q, r, organelleRot);
                    RenderHighlightedOrganelle(2, -1 * q, r + q, 6 + (-1 * organelleRot));
                    RenderHighlightedOrganelle(3, -1 * q, -1 * r, (organelleRot + 180) % 6);
                    RenderHighlightedOrganelle(4, q, -1 * (r + q),
                        8 + (-1 * organelleRot) % 6);
                    break;
                case 3:
                    RenderHighlightedOrganelle(1, q, r, organelleRot);
                    RenderHighlightedOrganelle(2, -1 * r, r + q, (organelleRot + 1) % 6);
                    RenderHighlightedOrganelle(3, -1 * (r + q), q, (organelleRot + 2) % 6);
                    RenderHighlightedOrganelle(4, -1 * q, -1 * r, (organelleRot + 3) % 6);
                    RenderHighlightedOrganelle(5, r, -1 * (r + q), (organelleRot + 4) % 6);
                    RenderHighlightedOrganelle(6, r + q, -1 * q, (organelleRot + 5) % 6);
                    break;
            }
        }
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int unused, int q, int r, int rotation)
    {
        if (ActiveActionName == null)
            return;

        // TODO: this should be changed into a function parameter
        var toBePlacedOrganelle = SimulationParameters.Instance.GetOrganelleType(
            ActiveActionName);

        bool showModel = true;

        foreach (var hex in toBePlacedOrganelle.GetRotatedHexes(rotation))
        {
            int posQ = hex.Q + q;
            int posR = hex.R + r;

            var pos = Hex.AxialToCartesian(new Hex(posQ, posR));

            // Detect can it be placed there
            bool canPlace = isPlacementProbablyValid;

            bool duplicate = false;

            // Skip if there is a placed organelle here already
            foreach (var placed in placedHexes)
            {
                if ((pos - placed.Translation).LengthSquared() < 0.001f)
                {
                    duplicate = true;

                    if (!canPlace)
                    {
                        // Mark as invalid
                        placed.MaterialOverride = invalidMaterial;

                        showModel = false;
                    }

                    break;
                }
            }

            if (duplicate)
                continue;

            var hoverHex = hoverHexes[usedHoverHex++];

            hoverHex.Translation = pos;
            hoverHex.Visible = true;

            hoverHex.MaterialOverride = canPlace ? validMaterial : invalidMaterial;
        }

        // Model
        if (toBePlacedOrganelle.DisplayScene != string.Empty && showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var organelleModel = hoverOrganelles[usedHoverOrganelle++];

            organelleModel.Transform = new Transform(
                MathUtils.CreateRotationForOrganelle(rotation),
                cartesianPosition + toBePlacedOrganelle.CalculateModelOffset());

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, toBePlacedOrganelle.DisplayScene);
        }
    }

    /// <summary>
    ///   Returns the hex position the mouse is over
    /// </summary>
    private void GetMouseHex(out int q, out int r)
    {
        // Get the position of the cursor in the plane that the microbes is floating in
        var cursorPos = camera.CursorWorldPos;

        // Convert to the hex the cursor is currently located over.
        var hex = Hex.CartesianToAxial(cursorPos);

        q = hex.Q;
        r = hex.R;
    }

    private MeshInstance CreateEditorHex()
    {
        var hex = (MeshInstance)hexScene.Instance();
        world.AddChild(hex);
        return hex;
    }

    private SceneDisplayer CreateEditorOrganelle()
    {
        var node = (SceneDisplayer)modelScene.Instance();
        world.AddChild(node);
        return node;
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor
    ///   and also applies symmetry to place multiple at once.
    /// </summary>
    private void AddOrganelle(string organelleType)
    {
        GetMouseHex(out int q, out int r);

        switch (Symmetry)
        {
            case 0:
                PlaceIfPossible(organelleType, q, r, organelleRot);
                break;
            case 1:
                PlaceIfPossible(organelleType, q, r, organelleRot);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot));
                }

                break;
            case 2:
                {
                    PlaceIfPossible(organelleType, q, r, organelleRot);

                    if (q != -1 * q || r != r + q)
                    {
                        PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot));
                        PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6);
                        PlaceIfPossible(organelleType, q, -1 * (r + q),
                            (8 + (-1 * organelleRot)) % 6);
                    }
                }

                break;
            case 3:
                {
                    PlaceIfPossible(organelleType, q, r, organelleRot);

                    if (q != -1 * q || r != r + q)
                    {
                        PlaceIfPossible(organelleType, -1 * r, r + q, (organelleRot + 1) % 6);
                        PlaceIfPossible(organelleType, -1 * (r + q), q,
                            (organelleRot + 2) % 6);
                        PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6);
                        PlaceIfPossible(organelleType, r, -1 * (r + q),
                            (organelleRot + 4) % 6);
                        PlaceIfPossible(organelleType, r + q, -1 * q, (organelleRot + 5) % 6);
                    }
                }

                break;
            default:
                throw new Exception("unimplemented symmetry in AddOrganelle");
        }
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private void PlaceIfPossible(string organelleType, int q, int r, int rotation)
    {
        var organelle = new OrganelleTemplate(GetOrganelleDefinition(organelleType),
            new Hex(q, r), organelleRot);

        if (!IsValidPlacement(organelle))
            return;

        // Skip placing if the player can't afford the organelle
        if (organelle.Definition.MPCost > MutationPoints && !FreeBuilding)
            return;

        AddOrganelle(organelle);
    }

    private bool IsValidPlacement(OrganelleTemplate organelle)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != "cytoplasm";
        return editedMicrobeOrganelles.CanPlaceAndIsTouching(organelle, notPlacingCytoplasm);
    }

    private OrganelleDefinition GetOrganelleDefinition(string name)
    {
        return SimulationParameters.Instance.GetOrganelleType(name);
    }

    private void DoOrganellePlaceAction(EditorAction action)
    {
        var data = (PlacementActionData)action.Data;

        data.ReplacedCytoplasm = new List<OrganelleTemplate>();
        var organelle = data.Organelle;

        // Check if there is cytoplasm under this organelle.
        foreach (var hex in organelle.RotatedHexes)
        {
            var organelleHere = editedMicrobeOrganelles.GetOrganelleAt(
                hex + organelle.Position);

            if (organelleHere == null)
                continue;

            if (organelleHere.Definition.Name != "cytoplasm")
            {
                throw new Exception("Can't place organelle on top of something " +
                    "else than cytoplasm");
            }

            // First we save the organelle data and then delete it
            data.ReplacedCytoplasm.Add(organelleHere);
            editedMicrobeOrganelles.Remove(organelleHere);
        }

        GD.Print("Placing organelle '", organelle.Definition.Name, "' at: ",
            organelle.Position);

        editedMicrobeOrganelles.Add(organelle);
    }

    private void UndoOrganellePlaceAction(EditorAction action)
    {
        var data = (PlacementActionData)action.Data;

        editedMicrobeOrganelles.Remove(data.Organelle);

        foreach (var cyto in data.ReplacedCytoplasm)
        {
            GD.Print("Replacing ", cyto.Definition.Name, " at: ",
                cyto.Position);

            editedMicrobeOrganelles.Add(cyto);
        }
    }

    private void AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put nucleus but you already have it
        // 2 - you put organelle that need nucleus and you don't have it
        if ((organelle.Definition.Name == "nucleus" && HasNucleus) ||
            ((organelle.Definition.ProkaryoteChance == 0 && !HasNucleus)
            && organelle.Definition.ChanceToCreate != 0))
            return;

        var action = new EditorAction(this, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction,
            new PlacementActionData(organelle));

        EnqueueAction(action);
    }

    private void DoOrganelleRemoveAction(EditorAction action)
    {
        var data = (RemoveActionData)action.Data;
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    private void UndoOrganelleRemoveAction(EditorAction action)
    {
        var data = (RemoveActionData)action.Data;
        editedMicrobeOrganelles.Add(data.Organelle);
    }

    private void RemoveOrganelleAt(Hex location)
    {
        var organelleHere = editedMicrobeOrganelles.GetOrganelleAt(location);
        if (organelleHere == null)
            return;

        // Dont allow deletion of nucleus or the last organelle
        // TODO: allow deleting the last cytoplasm if an organelle is about to be placed
        if (organelleHere.Definition.InternalName == "nucleus" || MicrobeSize < 2)
            return;

        int cost = Constants.ORGANELLE_REMOVE_COST;

        var action = new EditorAction(this, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction,
            new RemoveActionData(organelleHere));

        EnqueueAction(action);
    }

    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        OnOrganellesChanged();
    }

    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        OnOrganellesChanged();
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        // send to gui current status of cell
        gui.UpdateSize(MicrobeHexSize);
        gui.UpdateGuiButtonStatus(HasNucleus);

        // TODO: if this turns out to be expensive this should only be
        // called once the cell is fully loaded in and not for each
        // organelle
        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);

        // TODO: this might also be expensive
        gui.UpdateSpeed(CalculateSpeed());
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all
    ///   the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        int nextFreeHex = 0;
        int nextFreeOrganelle = 0;

        // Build the entities to show the current microbe
        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            foreach (var hex in organelle.RotatedHexes)
            {
                var pos = Hex.AxialToCartesian(hex + organelle.Position);

                if (nextFreeHex >= placedHexes.Count)
                {
                    // New hex needed
                    placedHexes.Add(CreateEditorHex());
                }

                var hexNode = placedHexes[nextFreeHex++];
                hexNode.MaterialOverride = validMaterial;
                hexNode.Translation = pos;

                hexNode.Visible = true;
            }

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var pos = Hex.AxialToCartesian(organelle.Position) +
                    organelle.Definition.CalculateModelOffset();

                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreateEditorOrganelle());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                organelleModel.Transform = new Transform(
                    MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), pos);

                organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                    Constants.DEFAULT_HEX_SIZE);

                organelleModel.Visible = true;

                UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene);
            }
        }

        // Delete excess entities
        while (nextFreeHex < placedHexes.Count)
        {
            placedHexes[placedHexes.Count - 1].QueueFree();
            placedHexes.RemoveAt(placedHexes.Count - 1);
        }

        while (nextFreeOrganelle < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].QueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it
    /// </summary>
    private void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel, string displayScene)
    {
        organelleModel.Scene = displayScene;
    }

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    private void EnqueueAction(EditorAction action)
    {
        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (MutationPoints < action.Cost)
            return;

        history.AddAction(action);

        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        gui.SetUndoButtonStatus(history.CanUndo());
        gui.SetRedoButtonStatus(history.CanRedo());
    }

    /// <summary>
    ///   Called once auto-evo results are ready
    /// </summary>
    private void OnEditorReady()
    {
        ready = true;
        gui.SetLoadingStatus(false);

        GD.Print("Elapsing time on editor entry");

        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);

        // Get summary before applying results in order to get comparisons to the previous populations
        var run = CurrentGame.GameWorld.GetAutoEvoRun();
        var summary = run.Results.MakeSummary(CurrentGame.GameWorld.Map, true, run.ExternalEffects);
        var external = run.MakeSummaryOfExternalEffects();

        gui.UpdateAutoEvoResults(summary, external);

        ApplyAutoEvoResults();
    }

    private void ApplyAutoEvoResults()
    {
        GD.Print("Applying auto-evo results");
        CurrentGame.GameWorld.GetAutoEvoRun().ApplyExternalEffects();

        CurrentGame.GameWorld.Map.RemoveExtinctSpecies(FreeBuilding);

        CurrentGame.GameWorld.Map.UpdateGlobalPopulations();

        // Clear the run to make the cell stage start a new run when we go back there
        CurrentGame.GameWorld.ResetAutoEvoRun();
    }

    /// <summary>
    ///   Updates the background shown in the editor
    /// </summary>
    private void UpdatePatchBackgroundImage()
    {
        camera.SetBackground(SimulationParameters.Instance.GetBackground(CurrentPatch.Biome.Background));
    }

    /// <summary>
    ///   Done actions are stored here to provide undo/redo functionality
    /// </summary>
    private class EditorAction : ReversableAction
    {
        public readonly int Cost;

        /// <summary>
        ///   Action specific data
        /// </summary>
        public object Data;

        private readonly Action<EditorAction> redo;
        private readonly Action<EditorAction> undo;

        private readonly MicrobeEditor editor;

        public EditorAction(MicrobeEditor editor, int cost,
            Action<EditorAction> redo,
            Action<EditorAction> undo, object data = null)
        {
            this.editor = editor;
            Cost = cost;
            this.redo = redo;
            this.undo = undo;
            Data = data;
        }

        public override void DoAction()
        {
            editor.ChangeMutationPoints(-Cost);
            redo(this);
        }

        public override void UndoAction()
        {
            editor.ChangeMutationPoints(Cost);
            undo(this);
        }
    }

    private class PlacementActionData
    {
        public List<OrganelleTemplate> ReplacedCytoplasm;
        public OrganelleTemplate Organelle;

        public PlacementActionData(OrganelleTemplate organelle)
        {
            Organelle = organelle;
        }
    }

    private class RemoveActionData
    {
        public OrganelleTemplate Organelle;

        public RemoveActionData(OrganelleTemplate organelle)
        {
            Organelle = organelle;
        }
    }
}
