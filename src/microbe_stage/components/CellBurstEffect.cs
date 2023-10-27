namespace Components
{
    public struct CellBurstEffect
    {
        /// <summary>
        ///   Radius of the effect, needs to be set before this gets initialized
        /// </summary>
        public float Radius;

        /// <summary>
        ///   Used by the burst system to detect which entities are not initialized yet
        /// </summary>
        public bool Initialized;

        public CellBurstEffect(float radius)
        {
            Radius = radius;
            Initialized = false;
        }
    }
}
