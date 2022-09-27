using Godot;

public class ThriveopediaEvolutionaryTreePage : ThriveopediaPage
{
    [Export]
    public NodePath EvolutionaryTreePath = null!;

    private EvolutionaryTree evolutionaryTree = null!;

    public override string PageName => "EVOLUTIONARY_TREE_PAGE";

    public override string TranslatedPageName => TranslationServer.Translate("EVOLUTIONARY_TREE_PAGE");

    public override void _Ready()
    {
        base._Ready();

        evolutionaryTree = GetNode<EvolutionaryTree>(EvolutionaryTreePath);
    }

    public override void UpdateCurrentWorldDetails()
    {
        if (CurrentGame == null)
            return;

        evolutionaryTree.Init(CurrentGame.GameWorld.PlayerSpecies);
    }
}