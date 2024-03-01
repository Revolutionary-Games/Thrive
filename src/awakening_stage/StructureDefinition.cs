using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;

[TypeConverter($"Saving.Serializers.{nameof(StructureStringConverter)}")]
public class StructureDefinition : BaseBuildableStructure
{
    [JsonConstructor]
    public StructureDefinition(string name) : base(name)
    {
    }

    [JsonProperty]
    public Vector3 InteractOffset { get; private set; }

    /// <summary>
    ///   The component factories which placed structures of this type should use
    /// </summary>
    [JsonProperty]
    public StructureComponentFactoryInfo Components { get; private set; } = new();

    public override void Check(string name)
    {
        base.Check(name);

        Components.Check(name);
    }

    /// <summary>
    ///   Checks if this structure contains a component of a given type
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     As the definition has the factories only this needs to be given the factory type that will create the
    ///     actual component, for example <see cref="HousingComponentFactory"/>
    ///   </para>
    /// </remarks>
    /// <typeparam name="T">The type of component factory to check for</typeparam>
    /// <returns>True if this has the specified component</returns>
    public bool HasComponentFactory<T>()
        where T : IStructureComponentFactory
    {
        foreach (var component in Components.Factories)
        {
            if (component is T)
                return true;
        }

        return false;
    }

    public override string ToString()
    {
        return "Structure type " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when

    public class StructureComponentFactoryInfo
    {
        private readonly List<IStructureComponentFactory> allFactories = new();

#pragma warning disable CS0649 // set from JSON
        [JsonProperty]
        private SocietyCenterComponentFactory? societyCenter;

        [JsonProperty]
        private WoodGathererFactory? woodGatherer;

        [JsonProperty]
        private RockGathererFactory? rockGatherer;

        [JsonProperty]
        private FoodGathererFactory? foodGatherer;

        [JsonProperty]
        private HousingComponentFactory? housing;

        [JsonProperty]
        private StructureStorageComponentFactory? storage;

        [JsonProperty]
        private ResearchComponentFactory? research;

        [JsonProperty]
        private FactoryComponentFactory? factory;
#pragma warning restore CS0649

        [JsonIgnore]
        public IReadOnlyList<IStructureComponentFactory> Factories => allFactories;

        /// <summary>
        ///   Checks and initializes the factory data
        /// </summary>
        public void Check(string name)
        {
            if (societyCenter != null)
                allFactories.Add(societyCenter);

            if (woodGatherer != null)
                allFactories.Add(woodGatherer);

            if (rockGatherer != null)
                allFactories.Add(rockGatherer);

            if (foodGatherer != null)
                allFactories.Add(foodGatherer);

            if (housing != null)
                allFactories.Add(housing);

            if (storage != null)
                allFactories.Add(storage);

            if (research != null)
                allFactories.Add(research);

            if (factory != null)
                allFactories.Add(factory);

            foreach (var componentFactory in allFactories)
            {
                componentFactory.Check(name);
            }
        }
    }
}
