using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each multicellular creature in the game
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/late_multicellular_stage/MulticellularCreature.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public class MulticellularCreature : RigidBody, ISpawned, IProcessable, ISaveLoadedTracked
{
    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses => throw new NotImplementedException();

    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => throw new NotImplementedException();

    // TODO: implement multicellular process statistics
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics => null;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public int DespawnRadiusSquared { get; set; }

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
