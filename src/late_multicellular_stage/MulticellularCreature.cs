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
    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    private Compound atp = null!;
    private Compound glucose = null!;

    [JsonProperty]
    private CreatureAI? ai;

    // TODO: implement
    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses => new();

    // TODO: implement
    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => compounds;

    // TODO: implement multicellular process statistics
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics => null;

    [JsonProperty]
    public bool Dead { get; private set; }

    [JsonProperty]
    public Action<MulticellularCreature>? OnDeath { get; set; }

    [JsonProperty]
    public Action<MulticellularCreature, bool>? OnReproductionStatus { get; set; }

    /// <summary>
    ///   The species of this creature. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public LateMulticellularSpecies Species { get; private set; } = null!;

    /// <summary>
    ///    True when this is the player's creature
    /// </summary>
    [JsonProperty]
    public bool IsPlayerCreature { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; } = null!;

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public int DespawnRadiusSquared { get; set; }

    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        base._Ready();

        atp = SimulationParameters.Instance.GetCompound("atp");
        glucose = SimulationParameters.Instance.GetCompound("glucose");
    }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(GameProperties currentGame, bool isPlayer)
    {
        CurrentGame = currentGame;
        IsPlayerCreature = isPlayer;

        if (!isPlayer)
            ai = new CreatureAI(this);

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // TODO: implement growth
        OnReproductionStatus?.Invoke(this, true);
    }

    public void ApplySpecies(Species species)
    {
        if (species is not LateMulticellularSpecies lateSpecies)
            throw new ArgumentException("Only late multicellular species can be used on creatures");

        Species = lateSpecies;

        // TODO: set from species
        compounds.Capacity = 100;

        // TODO: setup
    }

    public void SetInitialCompounds()
    {
        compounds.AddCompound(atp, 50);
        compounds.AddCompound(glucose, 50);
    }

    public MulticellularCreature SpawnOffspring()
    {
        var currentPosition = GlobalTransform.origin;

        // TODO: calculate size somehow
        var separation = new Vector3(10, 0, 0);

        // Create the offspring
        var copyEntity = SpawnHelpers.SpawnCreature(Species, currentPosition + separation,
            GetParent(), SpawnHelpers.LoadMulticellularScene(), true, CurrentGame);

        // Make it despawn like normal
        SpawnSystem.AddEntityToTrack(copyEntity);

        // TODO: some kind of resource splitting for the offspring?

        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");

        return copyEntity;
    }

    public void BecomeFullyGrown()
    {
        // TODO: implement growth
    }

    public void ResetGrowth()
    {
        // TODO: implement growth
    }

    public void Damage(float amount, string source)
    {
        if (IsPlayerCreature && CheatManager.GodMode)
            return;

        if (amount == 0 || Dead)
            return;

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("damage type is empty");

        // if (amount < 0)
        // {
        //     GD.PrintErr("Trying to deal negative damage");
        //     return;
        // }

        // TODO: sound

        // TODO: show damage visually
        // Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        // TODO: hitpoints and death
        // if (Hitpoints <= 0.0f)
        // {
        //     Hitpoints = 0.0f;
        //     Kill();
        // }
    }

    public void PlaySoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        // var sound = GD.Load<AudioStream>(effect);

        // TODO: implement sound playing, should probably create a helper method to share with Microbe

        /*// Find a player not in use or create a new one if none are available.
        var player = otherAudioPlayers.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (otherAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer3D();
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.UnitDb = GD.Linear2Db(volume);
        player.Stream = sound;
        player.Play();*/
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
