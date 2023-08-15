namespace Components
{
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Base properties of a microbe (separate from the species info as early multicellular species object couldn't
    ///   work there)
    /// </summary>
    public struct CellProperties
    {
        /// <summary>
        ///   Base colour of the cell. This is used when initializing organelles as it would otherwise be difficult to
        ///   to obtain the colour
        /// </summary>
        public Color Colour;

        public int HexCount;

        public float EngulfSize;

        public float UnadjustedRadius;

        public float RotationSpeed;

        // public float MassFromOrganelles

        public MembraneType MembraneType;
        public float MembraneRigidity;

        /// <summary>
        ///   The membrane created for this cell. This is here so that some other systems apart from the visuals system
        ///   can have access to the membrane data.
        /// </summary>
        [JsonIgnore]
        public Membrane? CreatedMembrane;

        public bool IsBacteria;

        public CellProperties(ICellProperties initialProperties)
        {
            Colour = initialProperties.Colour;
            HexCount = initialProperties.Organelles.HexCount;
            RotationSpeed = initialProperties.BaseRotationSpeed;
            MembraneType = initialProperties.MembraneType;
            MembraneRigidity = initialProperties.MembraneRigidity;
            CreatedMembrane = null;
            IsBacteria = initialProperties.IsBacteria;

            // These are initialized later
            EngulfSize = 0;
            UnadjustedRadius = 0;

            // TODO: do we need to copy some more properties?

            CalculateEngulfSize();
        }

        public float Radius => IsBacteria ? UnadjustedRadius * 0.5f : UnadjustedRadius;

        public void CalculateEngulfSize()
        {
            EngulfSize = IsBacteria ? HexCount * 0.5f : HexCount;
        }
    }
}
