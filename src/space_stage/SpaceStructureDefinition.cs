using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Saving.Serializers;

/// <summary>
///   Structure that can only be built in space / on an entire celestial body at once
/// </summary>
[TypeConverter($"Saving.Serializers.{nameof(SpaceStructureStringConverter)}")]
public class SpaceStructureDefinition : BaseBuildableStructure
{
    [JsonConstructor]
    public SpaceStructureDefinition(string name) : base(name)
    {
    }

    /// <summary>
    ///   The component factories which placed structures of this type should use
    /// </summary>
    [JsonProperty]
    public SpaceStructureComponentFactoryInfo Components { get; private set; } = new();

    public override void Check(string name)
    {
        base.Check(name);

        Components.Check(name);
    }

    /// <inheritdoc cref="StructureDefinition.HasComponentFactory{T}"/>>
    public bool HasComponentFactory<T>()
        where T : ISpaceStructureComponentFactory
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
        return "Space structure " + Name;
    }

    public class SpaceStructureComponentFactoryInfo
    {
        private readonly List<ISpaceStructureComponentFactory> allFactories = new();

#pragma warning disable CS0649 // set from JSON
        [JsonProperty]
        private AscensionComponentFactory? ascension;

        [JsonProperty]
        private InterplanetaryEnergyComponentFactory? interplanetaryEnergy;
#pragma warning restore CS0649

        [JsonIgnore]
        public IReadOnlyList<ISpaceStructureComponentFactory> Factories => allFactories;

        /// <summary>
        ///   Checks and initializes the factory data
        /// </summary>
        public void Check(string name)
        {
            if (ascension != null)
                allFactories.Add(ascension);

            if (interplanetaryEnergy != null)
                allFactories.Add(interplanetaryEnergy);

            foreach (var componentFactory in allFactories)
            {
                componentFactory.Check(name);
            }
        }
    }
}
