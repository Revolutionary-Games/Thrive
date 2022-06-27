using System;
using Godot;
using HarmonyLib;

public class AutoEvoExploringTool : ControlWithInput
{
    // All auto-evo config paths

    [Export]
    public NodePath AllowNoMutationPath = null!;

    [Export]
    public NodePath AllowNoMigrationPath = null!;

    [Export]
    public NodePath BiodiversityAttemptFillChancePath = null!;

    [Export]
    public NodePath BiodiversityFromNeighbourPatchChancePath = null!;

    [Export]
    public NodePath BiodiversityNearbyPatchIsFreePopulationPath = null!;

    [Export]
    public NodePath BiodiversitySplitIsMutatedPath = null!;

    [Export]
    public NodePath LowBiodiversityLimitPath = null!;

    [Export]
    public NodePath MaximumSpeciesInPatchPath = null!;

    [Export]
    public NodePath MoveAttemptsPerSpeciesPath = null!;

    [Export]
    public NodePath MutationsPerSpeciesPath = null!;

    [Export]
    public NodePath NewBiodiversityIncreasingSpeciesPopulationPath = null!;

    [Export]
    public NodePath ProtectMigrationsFromSpeciesCapPath = null!;

    [Export]
    public NodePath ProtectNewCellsFromSpeciesCapPath = null!;

    [Export]
    public NodePath RefundMigrationsInExtinctionsPath = null!;

    [Export]
    public NodePath StrictNicheCompetitionPath = null!;

    [Export]
    public NodePath SpeciesSplitByMutationThresholdPopulationAmountPath = null!;

    [Export]
    public NodePath SpeciesSplitByMutationThresholdPopulationFractionPath = null!;

    [Export]
    public NodePath UseBiodiversityForceSplitPath = null!;

    // All auto-evo config related controls.
    private CustomCheckBox allowNoMutationCheckBox = null!;
    private CustomCheckBox allowNoMigrationCheckBox = null!;
    private SpinBox biodiversityAttemptFillChanceCheckBox = null!;
    private SpinBox biodiversityFromNeighbourPatchChanceCheckBox = null!;
    private CustomCheckBox biodiversityNearbyPatchIsFreePopulationCheckBox = null!;
    private CustomCheckBox biodiversitySplitIsMutatedCheckBox = null!;
    private SpinBox lowBiodiversityLimitCheckBox = null!;
    private SpinBox maximumSpeciesInPatchCheckBox = null!;
    private SpinBox moveAttemptsPerSpeciesCheckBox = null!;
    private SpinBox mutationsPerSpeciesCheckBox = null!;
    private SpinBox newBiodiversityIncreasingSpeciesPopulationCheckBox = null!;
    private CustomCheckBox protectMigrationsFromSpeciesCapCheckBox = null!;
    private CustomCheckBox protectNewCellsFromSpeciesCapCheckBox = null!;
    private CustomCheckBox refundMigrationsInExtinctionsCheckBox = null!;
    private CustomCheckBox strictNicheCompetitionCheckBox = null!;
    private SpinBox speciesSplitByMutationThresholdPopulationAmountCheckBox = null!;
    private SpinBox speciesSplitByMutationThresholdPopulationFractionCheckBox = null!;
    private CustomCheckBox useBiodiversityForceSplitCheckBox = null!;

    private GameProperties? gameProperties;
    private AutoEvoConfiguration? autoEvoConfiguration;

    [Signal]
    public delegate void OnAutoEvoExploringToolClosed();

    public override void _Ready()
    {
        base._Ready();

        allowNoMutationCheckBox = GetNode<CustomCheckBox>(AllowNoMutationPath);
        allowNoMigrationCheckBox = GetNode<CustomCheckBox>(AllowNoMigrationPath);
        biodiversityAttemptFillChanceCheckBox = GetNode<SpinBox>(BiodiversityAttemptFillChancePath);
        biodiversityFromNeighbourPatchChanceCheckBox = GetNode<SpinBox>(BiodiversityFromNeighbourPatchChancePath);
        biodiversitySplitIsMutatedCheckBox = GetNode<CustomCheckBox>(BiodiversitySplitIsMutatedPath);
        biodiversityNearbyPatchIsFreePopulationCheckBox = GetNode<CustomCheckBox>(BiodiversityNearbyPatchIsFreePopulationPath);
        lowBiodiversityLimitCheckBox = GetNode<SpinBox>(LowBiodiversityLimitPath);
        maximumSpeciesInPatchCheckBox = GetNode<SpinBox>(MaximumSpeciesInPatchPath);
        moveAttemptsPerSpeciesCheckBox = GetNode<SpinBox>(MoveAttemptsPerSpeciesPath);
        mutationsPerSpeciesCheckBox = GetNode<SpinBox>(MutationsPerSpeciesPath);
        newBiodiversityIncreasingSpeciesPopulationCheckBox = GetNode<SpinBox>(NewBiodiversityIncreasingSpeciesPopulationPath);
        protectMigrationsFromSpeciesCapCheckBox = GetNode<CustomCheckBox>(ProtectMigrationsFromSpeciesCapPath);
        protectNewCellsFromSpeciesCapCheckBox = GetNode<CustomCheckBox>(ProtectNewCellsFromSpeciesCapPath);
        refundMigrationsInExtinctionsCheckBox = GetNode<CustomCheckBox>(RefundMigrationsInExtinctionsPath);
        strictNicheCompetitionCheckBox = GetNode<CustomCheckBox>(StrictNicheCompetitionPath);
        speciesSplitByMutationThresholdPopulationAmountCheckBox = GetNode<SpinBox>(SpeciesSplitByMutationThresholdPopulationAmountPath);
        speciesSplitByMutationThresholdPopulationFractionCheckBox = GetNode<SpinBox>(SpeciesSplitByMutationThresholdPopulationFractionPath);
        useBiodiversityForceSplitCheckBox = GetNode<CustomCheckBox>(UseBiodiversityForceSplitPath);
    }

    public void OpenFromMainMenu()
    {
        if (Visible)
            return;

        Init();

        Show();
    }

    private void Init()
    {
        gameProperties = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings());
        autoEvoConfiguration = (AutoEvoConfiguration)SimulationParameters.Instance.AutoEvoConfiguration.Clone();

        
    }

    [RunOnKeyDown("ui_cancel")]
    private void OnBackButtonPressed()
    {
        if (!Visible)
            return;

        EmitSignal(nameof(OnAutoEvoExploringToolClosed));
    }
}
