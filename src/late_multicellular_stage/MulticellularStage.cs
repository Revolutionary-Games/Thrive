using Newtonsoft.Json;

/// <summary>
///   Main class for managing the late multicellular stage
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularStage.tscn")]
[DeserializedCallbackTarget]
[UseThriveSerializer]
public class MulticellularStage : StageBase<Microbe>
{
    protected override IStageHUD BaseHUD => throw new System.NotImplementedException();

    public override void StartMusic()
    {
        throw new System.NotImplementedException();
    }

    public override void OnFinishLoading(Save save)
    {
        throw new System.NotImplementedException();
    }

    public override void MoveToEditor()
    {
        throw new System.NotImplementedException();
    }

    public override void OnSuicide()
    {
        throw new System.NotImplementedException();
    }

    protected override void SpawnPlayer()
    {
        throw new System.NotImplementedException();
    }

    protected override void AutoSave()
    {
        throw new System.NotImplementedException();
    }

    protected override void PerformQuickSave()
    {
        throw new System.NotImplementedException();
    }
}
