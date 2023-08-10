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
        public int HexCount;

        public float EngulfSize;

        public float UnadjustedRadius;

        public float RotationSpeed;

        // public float MassFromOrganelles

        public bool IsBacteria;

        public MembraneType MembraneType;
        public float MembraneRigidity;

        /// <summary>
        ///   True when <see cref="Membrane"/> has been created. Set to false if membrane properties or organelles
        ///   are changed
        /// </summary>
        [JsonIgnore]
        public bool MembraneVisualsCreated;

        public CellProperties(ICellProperties initialProperties)
        {
            HexCount = initialProperties.Organelles.HexCount;
            RotationSpeed = initialProperties.BaseRotationSpeed;
            IsBacteria = initialProperties.IsBacteria;
            MembraneType = initialProperties.MembraneType;
            MembraneRigidity = initialProperties.MembraneRigidity;
            MembraneVisualsCreated = false;

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
